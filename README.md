<div align="center">
  <img
    src="https://raw.githubusercontent.com/LizardByte/Themerr-jellyfin/refs/heads/master/themerr.png"
    alt="Themerr icon"
    width="256"
  />
  <h1 align="center">Themerr-jellyfin</h1>
  <h4 align="center">Jellyfin theme song plugin using ThemerrDB.</h4>
</div>

<div align="center">
  <a href="https://github.com/LizardByte/Themerr-jellyfin/actions/workflows/ci.yml?query=branch%3Amaster"><img src="https://img.shields.io/github/actions/workflow/status/lizardbyte/Themerr-jellyfin/ci.yml.svg?branch=master&label=build&logo=github&style=for-the-badge" alt="GitHub Workflow Status"></a>
  <a href="https://github.com/LizardByte/Themerr-jellyfin/releases/latest"><img src="https://img.shields.io/github/downloads/lizardbyte/Themerr-jellyfin/total.svg?style=for-the-badge&logo=github" alt="GitHub Releases"></a>
  <a href="https://hub.docker.com/r/lizardbyte/themerr-jellyfin"><img src="https://img.shields.io/docker/pulls/lizardbyte/themerr-jellyfin.svg?style=for-the-badge&logo=docker" alt="Docker"></a>
  <a href="https://codecov.io/gh/LizardByte/Themerr-jellyfin"><img src="https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fapp.lizardbyte.dev%2Fdashboard%2Fshields%2Fcodecov%2FThemerr-jellyfin.json&style=for-the-badge&logo=codecov" alt="Codecov"></a>
  <a href="https://sonarcloud.io/project/overview?id=LizardByte_Themerr-jellyfin"><img src="https://img.shields.io/sonar/quality_gate/LizardByte_Themerr-jellyfin.svg?server=https%3A%2F%2Fsonarcloud.io&style=for-the-badge&logo=sonarqubecloud&label=sonarcloud" alt="SonarCloud"></a>
</div>

## ℹ️ About

Themerr-jellyfin connects Jellyfin to [ThemerrDB](https://github.com/LizardByte/ThemerrDB), a community-maintained
database of theme songs. It downloads matching themes for movies and TV shows in your library, keeps Themerr-provided
themes up to date, and leaves user-provided `theme.mp3` files untouched.

LizardByte has the full documentation hosted on [Read the Docs](http://themerr-jellyfin.readthedocs.io/).

## 📦 Installation

The recommended installation method is to add the LizardByte plugin repository to Jellyfin. See the
[Jellyfin plugin documentation](https://jellyfin.org/docs/general/server/plugins/) for additional information about
installing plugins.

1. In Jellyfin, navigate to **Dashboard → Plugins → Repositories**.
2. Add a repository using this URL:

   ```text
   https://app.lizardbyte.dev/jellyfin-plugin-repo/manifest.json
   ```

3. Open the plugin **Catalog** and search for `Themerr`.
4. Select and install the plugin.
5. Restart Jellyfin.

Other installation methods are also available:

- **Portable:** Download `themerr-jellyfin.zip` from the
  [latest release](https://github.com/LizardByte/Themerr-jellyfin/releases/latest), extract it to your Jellyfin plugins
  directory, and restart Jellyfin. The portable archive supports Linux, macOS, and Windows.
- **Docker:** The images on [Docker Hub](https://hub.docker.com/repository/docker/lizardbyte/themerr-jellyfin) and
  [GitHub Container Registry](https://github.com/orgs/LizardByte/packages?repo_name=themerr-jellyfin) provide a
  LinuxServer.io Docker mod, not a standalone container. See the
  [Docker documentation](https://themerr-jellyfin.readthedocs.io/en/latest/about/docker.html) for configuration.
- **Source:** Installing from source is not recommended for most users. See the
  [build documentation](https://themerr-jellyfin.readthedocs.io/en/latest/contributing/build.html), then extract the
  generated zip archive to your Jellyfin plugins directory and restart Jellyfin.

## 🚀 Quick Start

1. In your Jellyfin user settings, select **Display**, then enable **Theme songs** in the **Library** section.
2. Ensure each movie and TV show is stored in its own directory. See Jellyfin's documentation for
   [movies](https://jellyfin.org/docs/general/server/media/movies/) and
   [TV shows](https://jellyfin.org/docs/general/server/media/shows/) for the required directory structure.
3. Open the Themerr [configuration page](http://localhost:8096/web/index.html#!/configurationpage?name=Themerr) and
   select **Update Theme Songs**. Alternatively, open Jellyfin's **Scheduled Tasks** page and select
   **Update Theme Songs** under **Themerr**.

After the initial run, Themerr runs automatically according to the schedule configured on its configuration page.

## 🛠️ Getting Help

Themerr messages are written to the Jellyfin server logs because Jellyfin does not maintain separate logs for plugins.
See the [Jellyfin log documentation](https://jellyfin.org/docs/general/administration/configuration#log-directory) to
locate them. Review logs for sensitive information before sharing them because Themerr does not control other
information logged by the Jellyfin server.

If you still need help, visit the [LizardByte Support Center](https://app.lizardbyte.dev/support).

## 🤝 Contributing

- Contribute themes through the [ThemerrDB repository](https://github.com/LizardByte/ThemerrDB).
- Contribute translations through [CrowdIn](https://translate.lizardbyte.dev/).
- For code contributions, see the [build](https://themerr-jellyfin.readthedocs.io/en/latest/contributing/build.html) and
  [testing](https://themerr-jellyfin.readthedocs.io/en/latest/contributing/testing.html) documentation.
