Testing
=======

flake8
------
Themerr-jellyfin uses `flake8 <https://pypi.org/project/flake8/>`__ for enforcing consistent code styling. flake8 is
included in the ``requirements-dev.txt``.

The config file for flake8 is ``.flake8``. This is already included in the root of the repo and should not be modified.

Test with flake8
   .. code-block:: bash

      python -m flake8

Sphinx
------
Themerr-jellyfin uses `Sphinx <https://www.sphinx-doc.org/en/master/>`__ for documentation building. Sphinx, along with
other required python dependencies are included in the `./docs/requirements.txt` file. Python is required to build
sphinx docs. Installation and setup of python will not be covered here.

.. todo::
   Add documentation within C# code to be included in sphinx docs.

The config file for Sphinx is ``docs/source/conf.py``. This is already included in the root of the repo and should not
be modified.

Test with Sphinx
   .. code-block:: bash

      cd docs
      make html

   Alternatively

   .. code-block:: bash

      cd docs
      sphinx-build -b html source build

Lint with rstcheck
   .. code-block:: bash

      rstcheck -r .

Unit Testing
------------
Themerr-jellyfin uses `xUnit <https://www.nuget.org/packages/xunit>`__ for unit testing.

Test with xUnit
   .. code-block:: bash

      dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover --logger "console;verbosity=detailed"
