# Azure DevOps Pipeline (Reference)

This folder contains a **sanitised** Azure DevOps pipeline definition derived from the research project.

Purpose:

- show how training + deployment were orchestrated in Azure DevOps
- provide a starting point for organisations that want to reproduce a similar MLOps setup

Non-goals:

- this folder is not configured to run in your Azure DevOps organisation without edits
- it does not include secrets, service connections, or organisation resource names

## Files

- `outlier-mlops-pipeline.yml`: Azure DevOps pipeline with stages for tests, AML training, and batch endpoint deployment
- `config.yml`: placeholder variables (service connection names, workspace, compute)
- `azureml/*.yml`: step templates for Azure ML CLI operations (install CLI, connect to workspace, create compute, register env/data, run pipeline, deploy/test)

## Adapting

1. Replace the placeholder variables in `config.yml`.
2. Update `endpoint_name` and any referenced YAML paths.
3. Ensure your Azure DevOps service connection has access to the target subscription/resource group.
4. Review all templates for compatibility with your organisation’s security and network policies.

This repository focuses on the locally executable workflow and sanitised artifacts. The Azure DevOps pipeline is provided as a reference snapshot.
