Build
=====
Compiling Themerr-jellyfin requires the following:

- `git <https://git-scm.com/>`__
- `.net9.0 SDK <https://dotnet.microsoft.com/en-us/download/dotnet/9.0>`__
- `python >=3.14 <https://www.python.org/downloads/>`__
- `uv <https://docs.astral.sh/uv/>`__

Clone
-----
Ensure `git <https://git-scm.com/>`__ is installed and run the following:

.. code-block:: bash

   git clone https://github.com/lizardbyte/themerr-jellyfin.git
   cd ./themerr-jellyfin

Setup Python
------------
Python dependencies are managed with ``uv``. Install uv outside this repository's ``.venv`` if it is not already
available:

.. code-block:: bash

   pipx install uv
   # or
   python -m pip install --user uv

If Python 3.14 is not already installed, install it with uv:

.. code-block:: bash

   uv python install 3.14

Install Dependencies
--------------------
Create or update the project ``.venv`` environment, including build dependencies:

.. code-block:: bash

   uv sync --only-group dev --no-install-project

Compile
-------

.. code-block:: bash

   uv run --no-sync python ./scripts/build_plugin.py --output ./build

The generated ``build.yaml`` is printed to stdout before JPRM runs and is left in the repository root
by default. Use ``--remove-build-yaml`` to delete it after the script finishes.

Release builds can pass an explicit plugin version:

.. code-block:: bash

   uv run --no-sync python ./scripts/build_plugin.py --version v1.2.3 --output ./build

Any generated ``build.yaml`` field can be overridden with a ``THEMERR_`` environment variable.
Use the field name in upper snake case, such as ``THEMERR_NAME``, ``THEMERR_TARGET_ABI``,
``THEMERR_FRAMEWORK``, ``THEMERR_VERSION``, or ``THEMERR_CHANGELOG``. ``THEMERR_ARTIFACTS`` accepts
a YAML list, a JSON string array, a newline-separated list, or a comma-separated list. CI sets
``THEMERR_CHANGELOG`` from the release body automatically.

When Python dependencies change, update the lock file and include it in the same pull request:

.. code-block:: bash

   uv lock

CI installs from ``uv.lock`` with ``uv sync --frozen``. To check the lock file and install build dependencies locally,
run:

.. code-block:: bash

   uv lock --check
   uv sync --frozen --only-group dev --no-install-project

Remote Build
------------
It may be beneficial to build remotely in some cases. This will enable easier building on different operating systems.

#. Fork the project
#. Activate workflows
#. Trigger the `CI` workflow manually
#. Download the artifacts from the workflow run summary
