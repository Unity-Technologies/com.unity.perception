.PHONY: help

help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

.DEFAULT_GOAL := help

GCP_PROJECT_ID := unity-ai-thea-test
TAG ?= latest

build: ## Build datasetinsights docker image
	@echo "Building docker image for datasetinsights with tag: $(TAG)"
	@docker build -t datasetinsights:$(TAG) .

push: ## Push datasetinsights docker image to registry
	@echo "Uploading docker image to GCS registry with tag: $(TAG)"
	@docker tag datasetinsights:$(TAG) gcr.io/$(GCP_PROJECT_ID)/datasetinsights:$(TAG) && \
	docker push gcr.io/$(GCP_PROJECT_ID)/datasetinsights:$(TAG)
