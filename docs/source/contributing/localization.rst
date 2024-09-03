Localization
============
Themerr-jellyfin and related LizardByte projects are being localized into various languages. The default language is
`en` (English).

 .. image:: https://app.lizardbyte.dev/dashboard/crowdin/LizardByte_graph.svg

CrowdIn
-------
The translations occur on `CrowdIn <https://translate.lizardbyte.dev/>`__. Anyone is free to contribute to
localization there.

**Translations Basics**
   - The brand names `LizardByte` and `Themerr` should never be translated.
   - Other brand names should never be translated.
     Examples:

     - Jellyfin

**CrowdIn Integration**
   How does it work?

   When a change is made to the source locale file, those strings get pushed to CrowdIn automatically.

   When translations are updated on CrowdIn, a push gets made to the `l10n_master` branch and a PR is made.
   Once PR is merged, all updated translations are part of the project and will be included in the
   next release.

Extraction
----------

Themerr-jellyfin uses a custom translation implementation for localizing the html config page.
The implementation uses a JSON key-value pair to map the strings to their respective translations.

The following is a simple example of how to use it.

- Add the string to `Locale/en.json`, in English.
   .. code-block:: json

      {
        "hello": "Hello!"
      }

   .. note:: The json keys should be sorted alphabetically. You can use `jsonabc <https://novicelab.org/jsonabc/>`__
      to sort the keys.

- Use the string in the config page.
   .. code-block:: html

      <p data-localize="hello">Hello!</p>

.. note::
   - The `data-localize` attribute should be the same as the key in the JSON file.
   - The `innerText` of the element should be the default English string, incase the translations cannot be properly
     loaded.
   - The `data-localize` attribute can be added to any element that supports `innerText`.
   - Once the page is loaded, the `innerText` will be replaced with their respective translations.
   - If the translation is not found, there will be a fallback to the default English string.

- Use the string in javascript.
   .. code-block:: javascript

      const hello = translate("hello");
