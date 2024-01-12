Usage
=====

Minimal setup is required to use Themerr-jellyfin. In addition to the installation, a few settings must be
configured.

Enable Themes
-------------

#. Navigate to your user settings page.
#. Select `Display` from the user section.
#. Within the `Library` section, ensure `Theme songs` is enabled.

Directory Structure
-------------------

.. Attention:: Jellyfin requires your media to be stored in separate subdirectories, with each movie/show in its
   own folder. See `Movies <https://jellyfin.org/docs/general/server/media/movies/>`__
   or `TV Shows <https://jellyfin.org/docs/general/server/media/shows/>`__ for more information.

Task Activation
---------------

Scheduled
^^^^^^^^^

Themerr will run automatically on a schedule. You can configure the schedule in the `configuration page`_.

Manual
^^^^^^

To initialize a download task manually, follow these steps:

#. Navigate to `configuration page`_.
#. Select `Update Theme Songs`.

Or alternatively:

#. Navigate to `<http://localhost:8096/web/index.html#!/scheduledtasks.html>`__.
#. Select `Update Theme Songs` under the `Themerr` section.

Theme Updates
-------------

Themerr will only add or update a theme song if the following conditions are met.

- A user supplied ``theme.mp3`` is not present.
- The theme in ThemerrDB is different from the previously added theme by Themerr.

.. _configuration page: http://localhost:8096/web/index.html#!/configurationpage?name=Themerr
