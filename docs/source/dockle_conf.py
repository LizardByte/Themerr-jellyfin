# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# standard imports
import xml.etree.ElementTree as ET


def get_dotnet_version(file_path: str):
    tree = ET.parse(file_path)
    root = tree.getroot()

    for item in root.iter('PropertyGroup'):
        for child in item:
            if 'TargetFramework' in child.tag:
                # Split the string on 'net' and return the second part
                return child.text.split('net')[1]

    return None


def get_package_version(file_path: str, package_name: str):
    tree = ET.parse(file_path)
    root = tree.getroot()

    for item in root.iter('PackageReference'):
        if item.attrib['Include'] == package_name:
            return item.attrib['Version']

    return None


def get_major_minor_version(version: str | None):
    if version is None:
        return None

    return '.'.join(version.split('.')[:2])


def get_jellyfin_source_version(version: str | None):
    """Convert a Jellyfin NuGet version to its matching source tag version."""
    if version is None:
        return None

    version_parts = version.split('.')
    if len(version_parts) == 3 and int(version_parts[0]) >= 12 and version_parts[2] == '0':
        return '.'.join(version_parts[:2])

    return version


# -- Project integration ------------------------------------------------------

root_dir = dockle_project_root
csproj_file = root_dir / 'Jellyfin.Plugin.Themerr' / 'Jellyfin.Plugin.Themerr.csproj'

dotnet_version = get_dotnet_version(file_path=csproj_file)
jellyfin_version = get_package_version(
    file_path=csproj_file,
    package_name="Jellyfin.Controller",
)
jellyfin_source_tag_version = get_jellyfin_source_version(jellyfin_version)
jellyfin_source_url = f'https://github.com/jellyfin/jellyfin/blob/v{jellyfin_source_tag_version}'
efcore_version = get_package_version(
    file_path=csproj_file,
    package_name='Microsoft.EntityFrameworkCore.Sqlite',
)
youtube_explode_version = get_package_version(
    file_path=csproj_file,
    package_name='YoutubeExplode',
)
efcore_doc_version = get_major_minor_version(efcore_version) or dotnet_version


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions.extend([
    'breathe',  # c# support for sphinx with doxygen, and sphinx-csharp
    'sphinx.ext.autosectionlabel',
    'sphinx.ext.graphviz',  # enable graphs for breathe
    'sphinx.ext.todo',  # enable to-do sections
    'sphinx.ext.viewcode',  # add links to view source code
    'sphinx_copybutton',  # add a copy button to code blocks
    'sphinx_csharp',  # c# directives
])

# Add any paths that contain templates here, relative to this directory.
# templates_path = ['_templates']

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = ['toc.rst']

# Extensions to include.
source_suffix = {
    '.rst': 'restructuredtext',
    '.md': 'markdown',
}


# extension config options
autosectionlabel_prefix_document = True  # Make sure the target is unique
breathe_default_project = 'Jellyfin.Plugin.Themerr'
breathe_projects = {
    'Jellyfin.Plugin.Themerr': str(root_dir / '_site' / 'api' / 'xml')
}
sphinx_csharp_test_links = True
todo_include_todos = True

# How to generate external doc links, replace %s with type. Use the format
#    'package name': ('direct link to %s', 'alternate backup link or search page')
sphinx_csharp_ext_search_pages = {
    'System': (
        f'https://learn.microsoft.com/en-us/dotnet/api/system.%s?view=net-{dotnet_version}',
    ),
    'System.Collections.Generic': (
        f'https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.%s?view=net-{dotnet_version}',
    ),
    'System.Net.Http': (
        f'https://learn.microsoft.com/en-us/dotnet/api/system.net.http.%s?view=net-{dotnet_version}',
    ),
    'Microsoft': (
        f'https://learn.microsoft.com/en-us/dotnet/api/microsoft.%s?view=dotnet-plat-ext-{dotnet_version}',
    ),
    'Microsoft.EntityFrameworkCore': (
        f'https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.%s'
        f'?view=efcore-{efcore_doc_version}',
        'https://learn.microsoft.com/en-us/search/?terms=microsoft.entityframeworkcore.%s',
    ),
    'Microsoft.EntityFrameworkCore.Infrastructure': (
        f'https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.%s'
        f'?view=efcore-{efcore_doc_version}',
        'https://learn.microsoft.com/en-us/search/?terms=microsoft.entityframeworkcore.infrastructure.%s',
    ),
    'Microsoft.EntityFrameworkCore.Migrations': (
        f'https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.%s'
        f'?view=efcore-{efcore_doc_version}',
        'https://learn.microsoft.com/en-us/search/?terms=microsoft.entityframeworkcore.migrations.%s',
    ),
    'YoutubeExplode': (
        f'https://www.nuget.org/packages/YoutubeExplode/{youtube_explode_version}#%s',
    ),
    'Jellyfin.Controller.MediaBrowser.Common.Configuration': (
        f'{jellyfin_source_url}/MediaBrowser.Common/Configuration/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Common.Plugins': (
        f'{jellyfin_source_url}/MediaBrowser.Common/Plugins/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Configuration': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/Configuration/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Entities': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/Entities/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Movies': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/Entities/Movies/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.TV': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/Entities/TV/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Library': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/Library/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Plugins': (
        f'{jellyfin_source_url}/MediaBrowser.Controller/Plugins/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Model.Plugins': (
        f'{jellyfin_source_url}/MediaBrowser.Model/Plugins/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Model.Serialization': (
        f'{jellyfin_source_url}/MediaBrowser.Model/Serialization/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Model.Tasks': (
        f'{jellyfin_source_url}/MediaBrowser.Model/Tasks/%s.cs',
    ),
}

# Types that are in an external package. Use the format
#   'package name': {
#      'Namespace1': ['Type1', 'Type2'],
sphinx_csharp_ext_type_map = {
    'System': {
        '': [
            'DateTime',
            'Guid',
            'IProgress',
            'TimeSpan',
        ],
        'IO': [
            'Stream',
        ],
        'Threading': [
            'CancellationToken',
            'Timer',
        ],
        'Threading.Tasks': [
            'Task',
            'ValueTask',
        ],
    },
    'System.Collections.Generic': {
        '': [
            'HashSet',
            'IReadOnlyList',
        ],
    },
    'System.Net.Http': {
        '': [
            'HttpClient',
        ],
    },
    'Microsoft': {
        'AspNetCore.Mvc': [
            'ActionResult',
            'ControllerBase',
        ],
        'Extensions.Logging': [
            'ILogger',
            'ILoggerFactory',
        ],
        'Extensions.DependencyInjection': [
            'IServiceCollection',
        ],
        'Extensions.Hosting': [
            'IHostedService',
        ],
    },
    'Microsoft.EntityFrameworkCore': {
        '': [
            'DbContext',
            'DbContextOptions',
            'DbContextOptionsBuilder',
            'DbSet',
            'ModelBuilder',
        ],
    },
    'Microsoft.EntityFrameworkCore.Infrastructure': {
        '': [
            'DbContextAttribute',
            'ModelSnapshot',
        ],
    },
    'Microsoft.EntityFrameworkCore.Migrations': {
        '': [
            'Migration',
            'MigrationBuilder',
        ],
    },
    'YoutubeExplode': {
        'Videos.Streams': [
            'IStreamInfo',
            'StreamManifest',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Common.Configuration': {
        '': [
            'ConfigurationStore',
            'ConfigurationUpdateEventArgs',
            'EncodingConfigurationExtensions',
            'IApplicationPaths',
            'IConfigurationFactory',
            'IConfigurationManager',
            'IValidatingConfiguration',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Common.Plugins': {
        '': [
            'BasePlugin',
            'BasePluginOfT',
            'IHasPluginConfiguration',
            'IPlugin',
            'IPluginAssembly',
            'IPluginManager',
            'LocalPlugin',
            'PluginManifest',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller': {
        '': [
            'IServerApplicationHost',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Configuration': {
        '': [
            'IServerConfigurationManager',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Entities': {
        '': [
            'BaseItem',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Library': {
        '': [
            'ILibraryManager',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Movies': {
        '': [
            'BoxSet',
            'Movie',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.TV': {
        '': [
            'Series',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Plugins': {
        '': [
            'IPluginServiceRegistrator',
            'IRunBeforeStartup',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Model.Plugins': {
        '': [
            'BasePluginConfiguration',
            'IHasWebPages',
            'PluginInfo',
            'PluginPageInfo',
            'PluginStatus',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Model.Serialization': {
        '': [
            'IXmlSerializer',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Model.Tasks': {
        '': [
            'IConfigurableScheduledTask',
            'IScheduledTask',
            'IScheduledTaskWorker',
            'ITaskManager',
            'ITaskTrigger',
            'ScheduledTaskHelpers',
            'TaskCompletionEventArgs',
            'TaskCompletionStatus',
            'TaskInfo',
            'TaskOptions',
            'TaskResults',
            'TaskState',
            'TaskTriggerInfo',
            'TaskTriggerInfoType',
        ],
    },
}

# [Advanced] Rename type before generating external link. Commonly used for generic types
sphinx_csharp_external_type_rename = {
    'DbContextOptions': 'DbContextOptions-1',
    'DbSet': 'DbSet-1',
    'HashSet': 'HashSet-1',
    'IProgress': 'IProgress-1',
    'IReadOnlyList': 'IReadOnlyList-1',
}

sphinx_csharp_ignore_xref = [
    '>',
]

# The README uses a raw HTML h1 to center its title, which MyST does not count when validating heading levels.
# disable epub mimetype and README heading warnings
# https://github.com/readthedocs/readthedocs.org/blob/eadf6ac6dc6abc760a91e1cb147cc3c5f37d1ea8/docs/conf.py#L235-L236
suppress_warnings = ["epub.unknown_project_files", "myst.header"]
