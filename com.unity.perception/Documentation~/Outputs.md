# Endpoints

[Dataset Capture](DatasetCapture.md) tracks sensors, annotations, and metrics, and delivers the data to an active endpoint. The endpoint is responsible for packaging data in a usable format for the user.

## Supported Endpoints

The Perception package comes with three inbuilt endpoint options.

### 1. (🌟 **Recommended** 🌟) SOLO Endpoint
> Link to our new and improved output schema: **[SOLO Endpoint Schema](Schema/SoloSchema.md)**

Our newest output format writes capture information into relevant directories as the simulation progresses. This differs from our previous method of writing data at the very end of simulation and enables you to peek at the data while a long simulation is running. Take a look at the schema page above to see the full list of benefits the SOLO endpoint has over our previous Perception endpoint.

### 2. Perception Endpoint

> Link to the legacy Perception schema: **[Perception Endpoint](Schema/PerceptionSchema.md)** 
    
Our legacy Perception output format where information is stored in captures and written out at the end of the simulation (or in 150 frame chunks). Although this endpoint still supports all the new Labelers such as depth and occlusion, we highly recommend trying out the SOLO endpoint above for a much easier post-processing and debugging experience.

### 3. No Output Endpoint

While selected, this endpoint does not write any information to disk! This is useful for preventing extraneous datasets from being generated while say debugging simulation logic in the editor or utilizing the Perception Camera's real-time visualization tools for testing.

## How to Change the Active Endpoint

1. Go to `Project Settings` → `Perception`
2. Click the `Change Endpoint Type` button
3. Choose your preferred endpoint from the drop-down.

## Creating your Own Endpoint

With the endpoint system, creating your own endpoints for your preferred output format is not just possible but easily accessible. We have put together a small example of a hypothetical custom endpoint to introduce you to the basics of creating, iterating on, and using your own custom endpoint. You can find the tutorial documentation here – [Custom Endpoints](Features/CustomEndpoints.md).
