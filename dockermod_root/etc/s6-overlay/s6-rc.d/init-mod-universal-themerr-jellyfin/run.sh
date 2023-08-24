﻿#!/usr/bin/with-contenv bash
# shellcheck shell=bash

# this is mostly borrowed from
# https://github.com/linuxserver/docker-baseimage-ubuntu/blob/8166223ec8da5012e4432776b5d6b882ffdc9c02/root/etc/s6-overlay/s6-rc.d/init-adduser/run

echo 'Initializing Themerr-jellyfin, as a mod for Linuxserver.io Jellyfin container'

echo '
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║          Themerr-jellyfin, a LizardByte application           ║
║                                                               ║
║  Docs: https://docs.lizardbyte.dev/projects/themerr-jellyfin  ║
║      Support center: https://app.lizardbyte.dev/#Support      ║
║          Donate: https://app.lizardbyte.dev/#Donate           ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
'

lsiown abc:abc /config/data/plugins/themerr-jellyfin
