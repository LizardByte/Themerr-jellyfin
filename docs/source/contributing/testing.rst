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

Sphinx
------
Themerr-jellyfin uses `Sphinx <https://www.sphinx-doc.org/en/master/>`__ for documentation building. Sphinx, along with
other required python dependencies are included in the ``docs`` dependency group in ``pyproject.toml``. Python
dependencies are managed with ``uv`` and locked in ``uv.lock``.

Install the documentation dependencies with:

.. code-block:: bash

   uv sync --only-group docs --no-install-project

The config file for Sphinx is ``docs/source/conf.py``. This is already included in the root of the repo and should not
be modified.

Test with Sphinx
   .. code-block:: bash

      cd docs
      uv run --no-sync make html

   Alternatively

   .. code-block:: bash

      cd docs
      uv run --no-sync sphinx-build -b html source build

Lint with rstcheck
   .. code-block:: bash

      uv run --no-sync rstcheck -r .

Unit Testing
------------
Themerr-jellyfin uses `xUnit <https://www.nuget.org/packages/xunit>`__ for unit testing.

Test with xUnit
   .. code-block:: bash

      dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover --logger "console;verbosity=detailed"
