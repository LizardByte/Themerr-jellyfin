Source Code
===========
Our source code is documented using the `standard documentation guidelines
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments>`__.

Source
------

.. doxygenindex::
   :allow-dot-graphs:

.. Ideally, we would use `doxygenfile` with `:allow-dot-graphs:`, but sphinx complains about duplicated namespaces...
..
.. .. toctree::
..    :caption: Jellyfin.Plugin.Themerr
..    :maxdepth: 1
..    :glob:
.. 
..    Jellyfin.Plugin.Themerr/*
.. 
.. .. toctree::
..    :caption: Jellyfin.Plugin.Themerr/Api
..    :maxdepth: 1
..    :glob:
.. 
..    Jellyfin.Plugin.Themerr/Api/*
.. 
.. .. toctree::
..    :caption: Jellyfin.Plugin.Themerr/Configuration
..    :maxdepth: 1
..    :glob:
.. 
..    Jellyfin.Plugin.Themerr/Configuration/*
.. 
.. .. toctree::
..    :caption: Jellyfin.Plugin.Themerr/ScheduledTasks
..    :maxdepth: 1
..    :glob:
.. 
..    Jellyfin.Plugin.Themerr/ScheduledTasks/*

.. Alternatively, can document the namespaces individually, but they don't support graphviz
..
.. .. doxygennamespace:: Jellyfin::Plugin::Themerr
..    :members:
..    :protected-members:
..    :private-members:
..    :undoc-members:
