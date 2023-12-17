# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html

# standard imports
from datetime import datetime
import os
import subprocess


# -- Path setup --------------------------------------------------------------

# If extensions (or modules to document with autodoc) are in another directory,
# add these directories to sys.path here. If the directory is relative to the
# documentation root, use os.path.abspath to make it absolute, like shown here.

script_dir = os.path.dirname(os.path.abspath(__file__))  # the directory of this file
source_dir = os.path.dirname(script_dir)  # the source folder directory
root_dir = os.path.dirname(source_dir)  # the root folder directory

# -- Project information -----------------------------------------------------
project = 'Themerr-jellyfin'
copyright = f'{datetime.now ().year}, {project}'
author = 'ReenigneArcher'

# The full version, including alpha/beta/rc tags
# https://docs.readthedocs.io/en/stable/reference/environment-variables.html#envvar-READTHEDOCS_VERSION
version = os.getenv('READTHEDOCS_VERSION', 'dirty')


# -- General configuration ---------------------------------------------------

# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
    'breathe',  # c# support for sphinx with doxygen, and sphinx-csharp
    'm2r2',  # enable markdown files
    'sphinx.ext.autosectionlabel',
    'sphinx.ext.graphviz',  # enable graphs for breathe
    'sphinx.ext.todo',  # enable to-do sections
    'sphinx.ext.viewcode',  # add links to view source code
    'sphinx_copybutton',  # add a copy button to code blocks
    'sphinx_csharp',  # c# directives
]

# Add any paths that contain templates here, relative to this directory.
# templates_path = ['_templates']

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = ['toc.rst']

# Extensions to include.
source_suffix = ['.rst', '.md']


# -- Options for HTML output -------------------------------------------------

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
# html_static_path = ['_static']

# These paths are either relative to html_static_path
# or fully qualified paths (eg. https://...)
# html_css_files = [
#     'css/custom.css',
# ]
# html_js_files = [
#     'js/custom.js',
# ]

html_logo = os.path.join(root_dir, 'themerr-jellyfin.png')

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
html_theme = 'furo'

html_theme_options = {
    "top_of_page_button": "edit",
    "source_edit_link": "https://github.com/lizardbyte/themerr-jellyfin/blob/master/docs/source/{filename}",
}

# extension config options
autosectionlabel_prefix_document = True  # Make sure the target is unique
breathe_default_project = 'Jellyfin.Plugin.Themerr'
breathe_projects = {
    "Jellyfin.Plugin.Themerr": "../build/doxyxml"
}
sphinx_csharp_test_links = True
todo_include_todos = True

# How to generate external doc links, replace %s with type. Use the format
#    'package name': ('direct link to %s', 'alternate backup link or search page')
sphinx_csharp_ext_search_pages = {
    'System': (
        'https://learn.microsoft.com/en-us/dotnet/api/system.%s?view=net-6.0',
    ),
    'Microsoft': (
        'https://learn.microsoft.com/en-us/dotnet/api/microsoft.%s?view=dotnet-plat-ext-6.0',
    ),
    'Jellyfin.Controller.MediaBrowser.Common.Configuration': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Common/Configuration/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Common.Plugins': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Common/Plugins/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Model.Plugins': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Model/Plugins/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Model.Serialization': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Model/Serialization/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Model.Tasks': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Model/Tasks/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Movies': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Controller/Entities/Movies/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Library': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Controller/Library/%s.cs',
    ),
    'Jellyfin.Controller.MediaBrowser.Controller.Plugins': (
        'https://github.com/jellyfin/jellyfin/blob/v10.8.13/MediaBrowser.Controller/Plugins/%s.cs',
    ),
}

# Types that are in an external package. Use the format
#   'package name': {
#      'Namespace1': ['Type1', 'Type2'],
sphinx_csharp_ext_type_map = {
    'System': {
        '': [
            'Guid',
            'IProgress',
        ],
        'Threading': [
            'CancellationToken',
            'Timer',
        ],
        'Threading.Tasks': [
            'Task',
        ],
    },
    'Microsoft': {
        'AspNetCore.Mvc': [
            'ControllerBase',
        ],
        'Extensions.Logging': [
            'ILogger',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Common.Configuration': {
        '': [
            'ConfigurationStore',
            'ConfigurationUpdateEventArgs',
            'EncodingConfigurationExtensions',
            'IApplicationPaths',
            'IConfigurationFactory',
            'IConfigurationManager',
            'IValidatingConfiguration',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Common.Plugins': {
        '': [
            'BasePlugin',
            'BasePluginOfT',
            'IHasPluginConfiguration',
            'IPlugin',
            'IPluginAssembly',
            'IPluginManager',
            'LocalPlugin',
            'PluginManifest',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Model.Plugins': {
        '': [
            'BasePluginConfiguration',
            'IHasWebPages',
            'PluginInfo',
            'PluginPageInfo',
            'PluginStatus',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Model.Serialization': {
        '': [
            'IXmlSerializer',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Model.Tasks': {
        '': [
            'IConfigurableScheduledTask',
            'IScheduledTask',
            'IScheduledTaskWorker',
            'ITaskManager',
            'ITaskTrigger',
            'ScheduledTaskHelpers',
            'TaskCompletionEventArgs',
            'TaskCompletionStatus',
            'TaskInfo',
            'TaskOptions',
            'TaskResults',
            'TaskState',
            'TaskTriggerInfo',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Movies': {
        '': [
            'BoxSet',
            'Movie',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Entities.Library': {
        '': [
            'ILibraryManager',
        ],
    },
    'Jellyfin.Controller.MediaBrowser.Controller.Plugins': {
        '': [
            'IRunBeforeStartup',
            'IServerEntryPoint',
        ],
    },
}

# [Advanced] Rename type before generating external link. Commonly used for generic types
sphinx_csharp_external_type_rename = {
    'IProgress': 'IProgress-1',
}

# disable epub mimetype warnings
# https://github.com/readthedocs/readthedocs.org/blob/eadf6ac6dc6abc760a91e1cb147cc3c5f37d1ea8/docs/conf.py#L235-L236
suppress_warnings = ["epub.unknown_project_files"]

# get doxygen version
doxy_proc = subprocess.run('doxygen --version', shell=True, cwd=source_dir, capture_output=True)
doxy_version = doxy_proc.stdout.decode('utf-8').strip()
print('doxygen version: ' + doxy_version)

# run doxygen
doxy_proc = subprocess.run('doxygen Doxyfile', shell=True, cwd=source_dir)
if doxy_proc.returncode != 0:
    raise RuntimeError('doxygen failed with return code ' + str(doxy_proc.returncode))
