Build
=====
Compiling Themerr-jellyfin requires the following:

- `git <https://git-scm.com/>`__
- `.net9.0 SDK <https://dotnet.microsoft.com/en-us/download/dotnet/9.0>`__
- `python >=3.14 <https://www.python.org/downloads/>`__

Clone
-----
Ensure `git <https://git-scm.com/>`__ is installed and run the following:

.. code-block:: bash

   git clone https://github.com/lizardbyte/themerr-jellyfin.git
   cd ./themerr-jellyfin

Setup Python venv
-----------------
It is recommended to setup and activate a `venv`_.

Install Requirements
--------------------
Install Requirements

.. code-block:: bash

   python -m pip install ".[dev]"

Compile
-------

.. code-block:: bash

   python ./scripts/build_plugin.py --output ./build

The generated ``build.yaml`` is printed to stdout before JPRM runs and is left in the repository root
by default. Use ``--remove-build-yaml`` to delete it after the script finishes.

Release builds can pass an explicit plugin version:

.. code-block:: bash

   python ./scripts/build_plugin.py --version v1.2.3 --output ./build

Any generated ``build.yaml`` field can be overridden with a ``THEMERR_`` environment variable.
Use the field name in upper snake case, such as ``THEMERR_NAME``, ``THEMERR_TARGET_ABI``,
``THEMERR_FRAMEWORK``, ``THEMERR_VERSION``, or ``THEMERR_CHANGELOG``. ``THEMERR_ARTIFACTS`` accepts
a YAML list, a JSON string array, a newline-separated list, or a comma-separated list. CI sets
``THEMERR_CHANGELOG`` from the release body automatically.

Remote Build
------------
It may be beneficial to build remotely in some cases. This will enable easier building on different operating systems.

#. Fork the project
#. Activate workflows
#. Trigger the `CI` workflow manually
#. Download the artifacts from the workflow run summary

.. _venv: https://docs.python.org/3/library/venv.html
