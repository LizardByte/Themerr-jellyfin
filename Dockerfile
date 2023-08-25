# syntax=docker/dockerfile:1.4
# artifacts: false
# platforms: linux/amd64,linux/arm64/v8
# cannot enable "linux/arm/v7" due to issue with dotnet
FROM ubuntu:22.04 AS base

ENV DEBIAN_FRONTEND=noninteractive

FROM base as buildstage

# build args
ARG BUILD_VERSION
ARG COMMIT
ARG GITHUB_SHA=$COMMIT
# note: BUILD_VERSION may be blank, COMMIT is also available

SHELL ["/bin/bash", "-o", "pipefail", "-c"]
# install dependencies
# dotnet deps: https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#dependencies
RUN <<_DEPS
#!/bin/bash
apt-get update -y
apt-get install -y --no-install-recommends \
  libc6 \
  libgcc-s1 \
  libgssapi-krb5-2 \
  libicu70 \
  liblttng-ust1 \
  libssl3 \
  libstdc++6 \
  libunwind8 \
  python3 \
  python3-pip \
  unzip \
  wget \
  zlib1g
apt-get clean
rm -rf /var/lib/apt/lists/*
_DEPS

# install dotnet-sdk
WORKDIR /tmp
RUN <<_DOTNET
#!/bin/bash
url="https://dot.net/v1/dotnet-install.sh"
wget --quiet "$url" -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 6.0
_DOTNET

# add dotnet to path
ENV PATH="${PATH}:/root/.dotnet"

# create build dir and copy GitHub repo there
COPY --link . /build

# set build dir
WORKDIR /build

# update pip
RUN <<_PIP
#!/bin/bash
python3 -m pip install --no-cache-dir --upgrade \
  pip setuptools wheel
python3 -m pip install --no-cache-dir -r requirements-dev.txt
_PIP

# build
RUN <<_BUILD
#!/bin/bash
# force the workflow to fail if any command fails
set -e
# jprm fails if output directory does not exist, so create it
mkdir -p ./artifacts
# check if build version is empty
if [ -z "${BUILD_VERSION}" ]; then
  BUILD_VERSION="0.0.0.0"
else
  # remove the v prefix from the version
  BUILD_VERSION="${BUILD_VERSION#v}"
fi
python3 -m jprm --verbosity=debug plugin build "./" --version="${BUILD_VERSION}" --output="./artifacts"
mkdir -p /artifacts
unzip ./artifacts/*.zip -d /artifacts
_BUILD

# apply permissions to s6 run script
RUN chmod +x ./dockermod_root/etc/s6-overlay/s6-rc.d/init-mod-jellyfin-themerr/run

FROM scratch AS deploy

# variables
ARG PLUGIN_NAME="themerr-jellyfin"
ARG PLUGIN_DIR="/config/data/plugins"

# add files from buildstage
# trailing slash on artifacts directory copies the contents of the directory, instead of the directory itself
COPY --link --from=buildstage /artifacts/ $PLUGIN_DIR/$PLUGIN_NAME

# copy s6 initialization files
COPY --link --from=buildstage /build/dockermod_root/ /
