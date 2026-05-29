# syntax=docker/dockerfile:1
# artifacts: false
# platforms: linux/amd64
# Linuxserver.io docker mods are not multiplatform, so no point in enabling "linux/arm64/v8"
# cannot enable "linux/arm/v7" due to issue with dotnet
FROM ubuntu:26.04 AS base

ENV DEBIAN_FRONTEND=noninteractive

FROM base AS buildstage

# build args
ARG BUILD_VERSION
ARG COMMIT
ARG GITHUB_SHA=$COMMIT
# note: BUILD_VERSION may be blank, COMMIT is also available

ENV VIRTUAL_ENV=/opt/venv
ENV UV_PROJECT_ENVIRONMENT=${VIRTUAL_ENV}

SHELL ["/bin/bash", "-o", "pipefail", "-c"]
# install dependencies
# dotnet deps: https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#dependencies
RUN <<_DEPS
#!/bin/bash
set -e
apt-get update -y
apt-get install -y --no-install-recommends \
  ca-certificates \
  curl \
  libbrotli1 \
  libc6 \
  libgcc-s1 \
  libgssapi-krb5-2 \
  libicu78 \
  libssl3t64 \
  libstdc++6 \
  python3 \
  python3-venv \
  tzdata \
  unzip \
  zlib1g
apt-get clean
rm -rf /var/lib/apt/lists/*
_DEPS

# install dotnet-sdk
WORKDIR /tmp
RUN <<_DOTNET
#!/bin/bash
set -e
url="https://dot.net/v1/dotnet-install.sh"
curl --proto "=https" --tlsv1.2 --silent --show-error --fail --location "$url" --output dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 9.0
_DOTNET

# install uv
RUN <<_UV
#!/bin/bash
set -e
url="https://astral.sh/uv/install.sh"
curl --proto "=https" --tlsv1.2 --silent --show-error --fail --location "$url" --output uv-install.sh
sh uv-install.sh
_UV

# add dotnet, uv, and the project environment to path
ENV PATH="/root/.dotnet:/root/.local/bin:${VIRTUAL_ENV}/bin:${PATH}"

# create build dir and copy GitHub repo there
COPY --link . /build

# set build dir
WORKDIR /build

# install Python dependencies
RUN <<_UV_SYNC
#!/bin/bash
set -e
uv sync --frozen --only-group dev --python python3 --no-python-downloads --no-build --no-install-project
_UV_SYNC

# build
RUN <<_BUILD
#!/bin/bash
# force the workflow to fail if any command fails
set -e
# build wrapper generates build.yaml and creates the output directory for jprm
uv run --frozen \
  --no-sync python \
  ./scripts/build_plugin.py \
  --version="${BUILD_VERSION}" \
  --output="./artifacts" \
  --verbosity=debug
mkdir -p /artifacts
unzip ./artifacts/*.zip -d /artifacts
_BUILD

# apply permissions to s6 run script
RUN chmod +x ./dockermod_root/etc/s6-overlay/s6-rc.d/init-mod-jellyfin-themerr-config/run

FROM scratch AS layer_stage

# variables
ARG PLUGIN_NAME="themerr-jellyfin"
ARG PLUGIN_DIR="/root-layer/config/data/plugins"

# add files from buildstage
# trailing slash on artifacts directory copies the contents of the directory, instead of the directory itself
COPY --link --from=buildstage /artifacts/ $PLUGIN_DIR/$PLUGIN_NAME

# copy s6 initialization files
COPY --link --from=buildstage /build/dockermod_root/ /root-layer

FROM scratch AS deploy

# Linuxserver.io docker mods require that mods be fully contained in a single layer

# copy s6 initialization files
COPY --link --from=layer_stage /root-layer/ /
