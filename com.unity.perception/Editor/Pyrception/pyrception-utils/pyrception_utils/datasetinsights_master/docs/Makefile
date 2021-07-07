# Makefile for Sphinx documentation

BUILDDIR      = build
SOURCEDIR     = source
TEMPLATEDIR   = $(SOURCEDIR)/_templates

.PHONY: help clean html

help:
	@echo "Please use \`make <target>' where <target> is one of"
	@echo "  html       to make standalone HTML files"

clean:
	rm -rf $(BUILDDIR)

apidoc:
	sphinx-apidoc --templatedir=$(TEMPLATEDIR) -o $(SOURCEDIR) -d 2 ../datasetinsights/ \
		../datasetinsights/commands \
		../datasetinsights/dashboard.py \
		../datasetinsights/constants.py

html:
	sphinx-build -b html $(SOURCEDIR) $(BUILDDIR)/html
	@echo
	@echo "Build finished. The HTML doumentation pages are in $(BUILDDIR)/html."
