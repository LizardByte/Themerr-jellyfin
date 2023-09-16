Installation
============
..
   The recommended method for running Themerr-jellyfin is to add the `repository`_ to Jellyfin.

.. Tip:: See `Jellyfin Plugins <https://jellyfin.org/docs/general/server/plugins/>`__ for more information about
   installing plugins. 

..
   Repository
   ----------

   #. In Jellyfin, go to `<http://localhost:8096/web/index.html#!/repositories.html>`__.
   #. Add the repository ``https://repo.lizardbyte.dev/jellyfin/manifest.json``.
   #. Go to Catalog and search for `Themerr`.
   #. Select and install the plugin.
   #. Restart Jellyfin

Portable
--------
The portable archive is cross platform, meaning Linux, macOS, and Windows are supported.

#. Download the ``themerr-jellyfin.zip`` from the `latest release`_
#. Extract the contents to your Jellyfin plugins directory.
#. Restart Jellyfin

Docker
------
Docker images are available on `Dockerhub`_ and `ghcr.io`_.

See :ref:`Docker <about/docker:docker>` for additional information.

Source
------
.. Caution:: Installing from source is not recommended most users.

#. Follow the steps in :ref:`Build <contributing/build:build>`.
#. Extract the generated zip archive to your Jellyfin plugins directory.
#. Restart Jellyfin

.. _latest release: https://github.com/LizardByte/Themerr-jellyfin/releases/latest
.. _Dockerhub: https://hub.docker.com/repository/docker/lizardbyte/themerr-jellyfin
.. _ghcr.io: https://github.com/orgs/LizardByte/packages?repo_name=themerr-jellyfin
