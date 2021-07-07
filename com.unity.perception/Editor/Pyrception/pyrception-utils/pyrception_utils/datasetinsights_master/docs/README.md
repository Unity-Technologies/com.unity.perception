Building documentation
======================

Run the following commands from `docs` directory.

Automatic generate of Sphinx sources using [sphinx-apidoc](https://www.sphinx-doc.org/en/master/man/sphinx-apidoc.html)

```bash
make apidoc
```

This command only applies to newly created modules. It will not update modules that already exist. You will have to modify `docs/datasetinsighs.module_name` manually.

To build html files, run

```bash
make html
```

You can browse the documentation by opening `build/html/index.html` file directly in any web browser.

Cleanup build html files

```bash
make clean
```

Known issues
------------

1. Some of the documents are written in markdown format. We use [recommonmark](https://github.com/readthedocs/recommonmark) to generate documentations. It uses [CommonMark](http://commonmark.org/) to convert markdown files to rst files. Due to it's limitation, links to headers cannot have `_` or `.`. If the header has either of those characters, they should be replaced by dashes `-`. e.g. if you have a header `#### annotation_definitions.json` in the markdown file, to link to that header the markdown needs to be `[click link](#annotation-definitions-json)`

2. `Readthedocs.org` does not currently support [poetry](https://python-poetry.org/) officially. Until then, we have to manually generated a `docs/requirements.txt` file when new requirements is added to the repo. This file can be generated using command:

```bash
poetry export --dev --without-hashes -f requirements.txt > docs/requirements.txt
```
