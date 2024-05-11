# Docker

## lizardbyte/themerr-jellyfin

This is a [docker-mod](https://linuxserver.github.io/docker-mods/) for
[jellyfin](https://hub.docker.com/r/linuxserver/jellyfin) which adds [Themerr-jellyfin](https://github.com/LizardByte/Themerr-jellyfin)
to jellyfin as a plugin, to be downloaded/updated during container start.

This image extends the jellyfin image, and is not intended to be created as a separate container.

### Installation

In jellyfin docker arguments, set an environment variable `DOCKER_MODS=lizardbyte/themerr-jellyfin:latest` or
`DOCKER_MODS=ghcr.io/lizardbyte/themerr-jellyfin:latest`

If adding multiple mods, enter them in an array separated by `|`, such as
`DOCKER_MODS=lizardbyte/themerr-jellyfin:latest|linuxserver/mods:other-jellyfin-mod`

### Supported Architectures

Linuxserver.io docker mods do not support multi-arch images; however this image should run on any architecture. If
you have issues with this image on a specific architecture, please open an issue on GitHub.
