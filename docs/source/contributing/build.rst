Build
=====
Compiling Themerr-jellyfin requires the following:

- `git <https://git-scm.com/>`_
- `.net6.0 SDK <https://dotnet.microsoft.com/en-us/download/dotnet/6.0>`_
- `python 3.x <https://www.python.org/downloads/>`_

Clone
-----
Ensure `git <https://git-scm.com/>`_ is installed and run the following:

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

   python -m pip install -r ./requirements-dev.txt

Compile
-------

.. code-block:: bash

   mkdir -p ./build
   python -m jprm plugin build --output ./build

Remote Build
------------
It may be beneficial to build remotely in some cases. This will enable easier building on different operating systems.

#. Fork the project
#. Activate workflows
#. Trigger the `CI` workflow manually
#. Download the artifacts from the workflow run summary

.. _venv: https://docs.python.org/3/library/venv.html
