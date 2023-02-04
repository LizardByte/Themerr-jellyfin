Usage
=====

Minimal setup is required to use Themerr-jellyfin. In addition to the installation, a few settings must be
configured.

Enable Themes
-------------

#. Navigate to your user settings page.
#. Select `Display` from the user section.
#. Within the `Library` section, ensure `Theme songs` is enabled.

Movie Directory Structure
-------------------------

.. Attention:: Jellyfin requires movies to be stored in separate subdirectories, with each movie in its own folder.

Task Activation
---------------

Manual
^^^^^^

To initialize a download task manually, follow these steps:

#. Navigate to `<http://localhost:8096/web/index.html#!/configurationpage?name=Themerr>`_.
#. Select `Download Theme Songs`.

Or alternatively:

#. Navigate to `<http://localhost:8096/web/index.html#!/scheduledtasks.html>`_.
#. Select `Download Theme Songs` under the `Themerr` section.

Scheduled
^^^^^^^^^

Themerr will run automatically every 24 hours.

Theme Updates
-------------

Themerr will only add or update a theme song if the following conditions are met.

- A user supplied ``theme.mp3`` is not present.
- The theme in ThemerrDB is different from the previously added theme by Themerr.
