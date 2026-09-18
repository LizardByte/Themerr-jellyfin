Testing
=======

SonarAnalyzer.CSharp
--------------------
Themerr-jellyfin uses `SonarAnalyzers.CSharp <https://www.nuget.org/packages/SonarAnalyzer.CSharp/>`__ to spot Bugs,
Vulnerabilities, and Code Smells in the project. This is run automatically as part of the build process.

The config file for SonarAnalyzers.CSharp is ``.editorconfig``.

StyleCop.Analyzers
------------------
Themerr-jellyfin uses `StyleCop.Analyzers <https://www.nuget.org/packages/StyleCop.Analyzers/>`__ to enforce consistent
code styling. This is run automatically as part of the build process.

The config file for StyleCop.Analyzers is ``.editorconfig``.

Dockle
------
Themerr-jellyfin uses Dockle to build the Sphinx guide and the native Doxygen API reference from ``dockle.toml``.

Install the current prerelease dependencies with:

.. code-block:: bash

   python -m pip install 'lizardbyte-dockle[all] @ https://github.com/LizardByte/dockle/releases/download/v2026.918.203650/lizardbyte_dockle-2026.918.203650-py3-none-any.whl'

Dockle generates both native configuration files; do not add a project ``conf.py`` or ``Doxyfile``.

Test the documentation
   .. code-block:: bash

      PYTHONPATH=third-party/dockle/src python -m dockle check
      PYTHONPATH=third-party/dockle/src python -m dockle build

Unit Testing
------------
Themerr-jellyfin uses `xUnit <https://www.nuget.org/packages/xunit.v3>`__ for unit testing.

Test with xUnit
   .. code-block:: bash

      dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover --logger "console;verbosity=detailed"
