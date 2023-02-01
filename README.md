<h1 align="center">Jellyfin Theme Songs Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.media">Jellyfin Project</a></h3>

<p align="center">
Jellyfin Theme Songs plugin is a plugin that automatically downloads every theme song of your tv show library;

</p>

## Install Process


## From Repository
1. In jellyfin, go to dashboard -> plugins -> Repositories -> add and paste this link https://raw.githubusercontent.com/danieladov/JellyfinPluginManifest/master/manifest.json
2. Go to Catalog and search for Theme Songs
3. Click on it and install
4. Restart Jellyfin


## From .zip file
1. Download the .zip file from release page
2. Extract it and place the .dll file in a folder called ```plugins/Theme Songs``` under  the program data directory or inside the portable install directory
3. Restart Jellyfin

## User Guide
1. To download the theme songs you can do it from Schedule task or directly from the configuration of the plugin.





## Build Process
1. Clone or download this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build plugin with following command.
```sh
dotnet publish --configuration Release --output bin
```
4. Place the resulting .dll file in a folder called ```plugins/Merge versions``` under  the program data directory or inside the portable install directory


