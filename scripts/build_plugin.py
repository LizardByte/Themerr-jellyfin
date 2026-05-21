"""Generate JPRM build metadata and build the Themerr Jellyfin plugin."""

# standard imports
import argparse
import json
import os
import re
import shlex
import subprocess
import sys
import xml.etree.ElementTree as ET

# lib imports
import yaml


REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEFAULT_PROJECT_FILE = os.path.join(REPO_ROOT, "Jellyfin.Plugin.Themerr", "Jellyfin.Plugin.Themerr.csproj")
DEFAULT_PLUGIN_SOURCE_FILE = os.path.join(REPO_ROOT, "Jellyfin.Plugin.Themerr", "ThemerrPlugin.cs")
BUILD_YAML_FILE = os.path.join(REPO_ROOT, "build.yaml")
JELLYFIN_PACKAGE_NAME = "Jellyfin.Controller"
PROPERTY_REFERENCE_PATTERN = re.compile(r"\$\(([A-Za-z0-9_.-]+)\)")
GUID_PATTERN = re.compile(
    r"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b"
)

STATIC_BUILD_CONFIG = {
    "name": "Themerr",
    "image": "themerr-jellyfin.png",
    "overview": "Automatically add theme songs to movies and tv shows using ThemerrDB",
    "description": "Automatically add theme songs to movies and tv shows using ThemerrDB",
    "category": "Metadata",
    "owner": "LizardByte",
}

ARTIFACTS = [
    "Jellyfin.Plugin.Themerr.dll",
    "YoutubeExplode.dll",
    "AngleSharp.dll",
    "JsonExtensions.dll",
]

DEFAULT_CHANGELOG = "see LizardByte/Themerr-jellyfin on GitHub"
BUILD_YAML_KEYS = [
    "name",
    "image",
    "guid",
    "targetAbi",
    "framework",
    "overview",
    "description",
    "category",
    "owner",
    "artifacts",
    "version",
    "changelog",
]


class BuildYamlDumper(yaml.SafeDumper):
    """YAML dumper used for generated JPRM config."""

    def increase_indent(self, flow: bool = False, indentless: bool = False) -> None:
        """Indent sequence values under their parent key."""
        return super().increase_indent(flow, False)


class LiteralString(str):
    """String that should be emitted as a YAML literal block."""


class QuotedString(str):
    """String that should be emitted as a quoted YAML scalar."""


def literal_string_representer(dumper: yaml.Dumper, data: LiteralString) -> yaml.nodes.ScalarNode:
    """Represent a string as a YAML literal block."""
    return dumper.represent_scalar("tag:yaml.org,2002:str", data, style="|")


def quoted_string_representer(dumper: yaml.Dumper, data: QuotedString) -> yaml.nodes.ScalarNode:
    """Represent a string as a quoted YAML scalar."""
    return dumper.represent_scalar("tag:yaml.org,2002:str", data, style='"')


BuildYamlDumper.add_representer(LiteralString, literal_string_representer)
BuildYamlDumper.add_representer(QuotedString, quoted_string_representer)


def parse_args() -> argparse.Namespace:
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "path",
        nargs="?",
        default=REPO_ROOT,
        help="Plugin repository path passed to jprm.",
    )
    parser.add_argument(
        "-o",
        "--output",
        default="./build",
        help="Directory where jprm writes the plugin package.",
    )
    parser.add_argument(
        "-v",
        "--version",
        default=os.environ.get("BUILD_VERSION", "0.0.0.0"),
        help="Plugin version. Empty values and a lone 'v' become 0.0.0.0; a leading v is stripped.",
    )
    parser.add_argument(
        "--dotnet-configuration",
        default=os.environ.get("DOTNET_CONFIGURATION", "Release"),
        help="Dotnet build configuration passed to jprm.",
    )
    parser.add_argument(
        "--project-file",
        default=DEFAULT_PROJECT_FILE,
        help="Plugin csproj file used to derive Jellyfin ABI and target framework.",
    )
    parser.add_argument(
        "--plugin-source-file",
        default=DEFAULT_PLUGIN_SOURCE_FILE,
        help="Plugin source file used to derive the Jellyfin plugin guid.",
    )
    parser.add_argument(
        "--verbosity",
        default=os.environ.get("JPRM_VERBOSITY", "debug"),
        help="JPRM verbosity level.",
    )
    parser.add_argument(
        "--max-cpu-count",
        type=int,
        help="Maximum number of CPU cores passed to jprm.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Generate build.yaml and print the jprm command without running it.",
    )
    parser.add_argument(
        "--remove-build-yaml",
        action="store_true",
        help="Remove the generated build.yaml after the script finishes.",
    )
    return parser.parse_args()


def local_name(tag: str) -> str:
    """Return an XML element name without its namespace."""
    return tag.rsplit("}", maxsplit=1)[-1]


def load_project(project_file: str) -> ET.Element:
    """Load a csproj XML file."""
    if not os.path.isfile(project_file):
        raise FileNotFoundError(f"Project file not found: {project_file}")

    try:
        return ET.parse(project_file).getroot()
    except ET.ParseError as exc:
        raise ValueError(f"Unable to parse project file {project_file}: {exc}") from exc


def collect_msbuild_properties(project: ET.Element) -> dict[str, str]:
    """Collect simple PropertyGroup values from a project file."""
    properties: dict[str, str] = {}

    for group in project:
        if local_name(group.tag) != "PropertyGroup":
            continue

        for element in group:
            text = element.text.strip() if element.text else ""
            if text:
                properties[local_name(element.tag)] = text

    return properties


def expand_msbuild_properties(value: str, properties: dict[str, str]) -> str:
    """Expand $(PropertyName) references that point at simple project properties."""

    def replace_property(match: re.Match[str]) -> str:
        property_name = match.group(1)
        try:
            return properties[property_name]
        except KeyError as exc:
            raise ValueError(f"Unable to resolve MSBuild property reference $({property_name})") from exc

    return PROPERTY_REFERENCE_PATTERN.sub(replace_property, value)


def is_package_reference(element: ET.Element, package_name: str) -> bool:
    """Return whether an XML element is the requested PackageReference."""
    include = element.attrib.get("Include") or element.attrib.get("Update")
    return local_name(element.tag) == "PackageReference" and include == package_name


def find_package_reference(project: ET.Element, package_name: str) -> ET.Element | None:
    """Find a PackageReference element."""
    return next((element for element in project.iter() if is_package_reference(element, package_name)), None)


def package_reference_version(package_reference: ET.Element) -> str | None:
    """Return the version from a PackageReference element."""
    version = package_reference.attrib.get("Version")
    if version:
        return version.strip()

    for child in package_reference:
        if local_name(child.tag) == "Version" and child.text:
            return child.text.strip()

    return None


def find_package_version(project: ET.Element, package_name: str, properties: dict[str, str]) -> str:
    """Find a PackageReference version."""
    package_reference = find_package_reference(project, package_name)
    if package_reference is None:
        raise ValueError(f"PackageReference {package_name} was not found")

    version = package_reference_version(package_reference)
    if not version:
        raise ValueError(f"PackageReference {package_name} does not declare a Version")

    return expand_msbuild_properties(version, properties)


def find_target_framework(properties: dict[str, str]) -> str:
    """Find the target framework from a project file's properties."""
    target_framework = properties.get("TargetFramework")
    if target_framework:
        return expand_msbuild_properties(target_framework, properties)

    target_frameworks = properties.get("TargetFrameworks")
    if target_frameworks:
        expanded = expand_msbuild_properties(target_frameworks, properties)
        frameworks = [framework.strip() for framework in expanded.split(";") if framework.strip()]
        if frameworks:
            return frameworks[0]

    raise ValueError("Project file does not declare TargetFramework or TargetFrameworks")


def target_abi_from_jellyfin_version(jellyfin_version: str) -> str:
    """Convert the Jellyfin package version into the four-part targetAbi value JPRM expects."""
    version_match = re.match(r"^(\d+)(?:\.(\d+))?(?:\.(\d+))?(?:\.(\d+))?", jellyfin_version.strip())
    if not version_match:
        raise ValueError(f"Unable to determine targetAbi from Jellyfin version: {jellyfin_version}")

    parts = [part if part is not None else "0" for part in version_match.groups()]
    return ".".join(parts)


def find_plugin_guid(plugin_source_file: str) -> str:
    """Find the plugin GUID from the source file that declares the plugin Id."""
    if not os.path.isfile(plugin_source_file):
        raise FileNotFoundError(f"Plugin source file not found: {plugin_source_file}")

    with open(plugin_source_file, encoding="utf-8") as source_file:
        source = source_file.read()

    guids = sorted(set(GUID_PATTERN.findall(source)))
    if len(guids) != 1:
        raise ValueError(f"Expected one GUID in {plugin_source_file}, found {len(guids)}")

    return guids[0].lower()


def normalize_plugin_version(version: str | None) -> str:
    """Normalize plugin versions used by release jobs and local builds."""
    normalized = (version or "").strip()
    if not normalized or normalized.lower() == "v":
        return "0.0.0.0"
    if normalized.lower().startswith("v"):
        return normalized[1:]
    return normalized


def env_suffix_for_build_yaml_key(key: str) -> str:
    """Convert a build.yaml key to its THEMERR_ environment variable suffix."""
    return re.sub(r"(?<!^)([A-Z])", r"_\1", key).upper()


def env_names_for_build_yaml_key(key: str) -> list[str]:
    """Return supported environment variable names for a build.yaml key."""
    names = [f"THEMERR_{env_suffix_for_build_yaml_key(key)}"]
    compact_name = f"THEMERR_{key.upper()}"
    if compact_name not in names:
        names.append(compact_name)
    return names


def build_yaml_env_override(key: str) -> str | None:
    """Return the configured environment override for a build.yaml key."""
    for env_name in env_names_for_build_yaml_key(key):
        if env_name in os.environ:
            return os.environ[env_name]
    return None


def parse_artifacts_override(raw_artifacts: str) -> list[str]:
    """Parse THEMERR_ARTIFACTS into a YAML string list."""
    stripped = raw_artifacts.strip()
    if not stripped:
        return []

    if stripped.startswith("["):
        try:
            artifacts = json.loads(stripped)
        except json.JSONDecodeError as exc:
            raise ValueError(f"THEMERR_ARTIFACTS is not valid JSON: {exc}") from exc

        if not isinstance(artifacts, list) or not all(isinstance(artifact, str) for artifact in artifacts):
            raise ValueError("THEMERR_ARTIFACTS JSON must be an array of strings")

        return artifacts

    if "\n" in raw_artifacts:
        return [artifact.strip() for artifact in raw_artifacts.splitlines() if artifact.strip()]

    separator = ";" if ";" in raw_artifacts else ","
    return [artifact.strip() for artifact in raw_artifacts.split(separator) if artifact.strip()]


def apply_build_yaml_env_overrides(config: dict[str, str], artifacts: list[str]) -> tuple[dict[str, str], list[str]]:
    """Apply THEMERR_* overrides for generated build.yaml fields."""
    overridden_config = dict(config)
    overridden_artifacts = list(artifacts)

    for key in BUILD_YAML_KEYS:
        override = build_yaml_env_override(key)
        if override is None:
            continue

        if key == "artifacts":
            overridden_artifacts = parse_artifacts_override(override)
        else:
            overridden_config[key] = override

    return overridden_config, overridden_artifacts


def yaml_string(value: str) -> LiteralString | QuotedString:
    """Convert a string value to the preferred YAML scalar representation."""
    if "\n" in value:
        return LiteralString(value)
    return QuotedString(value)


def render_build_yaml(config: dict[str, str], artifacts: list[str]) -> str:
    """Render the complete JPRM build.yaml content."""
    ordered_keys = [
        "name",
        "image",
        "guid",
        "targetAbi",
        "framework",
        "overview",
        "description",
        "category",
        "owner",
    ]

    build_config: dict[str, str | list[str]] = {}
    for key in ordered_keys:
        build_config[key] = yaml_string(config[key])

    build_config["artifacts"] = [yaml_string(artifact) for artifact in artifacts]
    build_config["version"] = yaml_string(config["version"])
    build_config["changelog"] = yaml_string(config["changelog"])
    return yaml.dump(
        build_config,
        Dumper=BuildYamlDumper,
        allow_unicode=True,
        default_flow_style=False,
        explicit_start=True,
        sort_keys=False,
    )


def write_build_yaml(
    project_file: str,
    plugin_source_file: str,
    plugin_version: str,
) -> tuple[str, dict[str, str], list[str]]:
    """Generate and write build.yaml for JPRM."""
    project = load_project(project_file)
    properties = collect_msbuild_properties(project)
    jellyfin_version = find_package_version(project, JELLYFIN_PACKAGE_NAME, properties)
    target_framework = find_target_framework(properties)
    plugin_guid = find_plugin_guid(plugin_source_file)

    config = {
        **STATIC_BUILD_CONFIG,
        "guid": plugin_guid,
        "targetAbi": target_abi_from_jellyfin_version(jellyfin_version),
        "framework": target_framework,
        "version": plugin_version,
        "changelog": DEFAULT_CHANGELOG,
    }
    config, artifacts = apply_build_yaml_env_overrides(config, ARTIFACTS)

    build_yaml = render_build_yaml(config, artifacts)
    with open(BUILD_YAML_FILE, "w", encoding="utf-8") as build_yaml_file:
        build_yaml_file.write(build_yaml)

    print(
        f"Generated {os.path.basename(BUILD_YAML_FILE)}: targetAbi={config['targetAbi']}, "
        f"framework={config['framework']}, version={config['version']}",
        flush=True,
    )
    print(build_yaml, end="", flush=True)
    return build_yaml, config, artifacts


def resolve_repo_relative_path(path_value: str) -> str:
    """Resolve a path relative to the repository root."""
    if os.path.isabs(path_value):
        return os.path.abspath(path_value)
    return os.path.abspath(os.path.join(REPO_ROOT, path_value))


def build_jprm_command(args: argparse.Namespace, plugin_version: str, target_framework: str) -> list[str]:
    """Create the jprm command."""
    output_path = resolve_repo_relative_path(args.output)
    os.makedirs(output_path, exist_ok=True)

    command = [sys.executable, "-m", "jprm"]
    if args.verbosity:
        command.extend(["--verbosity", args.verbosity])

    command.extend(
        [
            "plugin",
            "build",
            resolve_repo_relative_path(args.path),
            "--version",
            plugin_version,
            "--output",
            output_path,
            "--dotnet-configuration",
            args.dotnet_configuration,
            "--dotnet-framework",
            target_framework,
        ]
    )

    if args.max_cpu_count is not None:
        command.extend(["--max-cpu-count", str(args.max_cpu_count)])

    return command


def main() -> int:
    """Script entrypoint."""
    args = parse_args()
    project_file = resolve_repo_relative_path(args.project_file)
    plugin_source_file = resolve_repo_relative_path(args.plugin_source_file)
    plugin_version = normalize_plugin_version(args.version)
    _, config, _ = write_build_yaml(project_file, plugin_source_file, plugin_version)
    command = build_jprm_command(args, config["version"], config["framework"])

    try:
        if args.dry_run:
            print("JPRM command:", shlex.join(command))
            return 0

        subprocess.run(command, cwd=REPO_ROOT, check=True)
        return 0
    finally:
        if args.remove_build_yaml and os.path.exists(BUILD_YAML_FILE):
            os.remove(BUILD_YAML_FILE)


if __name__ == "__main__":
    sys.exit(main())
