# Metadata Reporter Labeler

Often you may want to collect additional metadata on each object that is labeled in your scene for filtering. For
example, in the case of labeling different types of cars with the same label "Car", we can store the color, brand,
number of tyres, etc. as additional metadata. We can then use the metadata to maybe filter out all the red cars or the
cars which have three tyres.

To do so previously, you would need to create a custom labeler, handle the registration, creation, and management of
annotation data, and finally how those values get serialized and written to disk. With the new `MetadataReporterLabeler`
, that process should be as easy as adding a component to your GameObject and pressing play.

## How it works

Once added to the Perception Camera, the `MetadataReporterLabeler` listens for any component which inherits from
the `BaseMetadataTag` class. Every camera capture, the `MetadataReporterLabeler` asks all those components to report
back values and then serializes those values as in a collated metric.

For example, if you add a `LabelingTransformDataMetadataTag` component to a GameObject in the scene, information about
the GameObject's transform will be automatically reported (and in this case, tied to its instance id) in a metric for the captured frame as such:

```json
{
  "@type": "type.unity.com/unity.solo.GenericMetric",
  "id": "metadata",
  "sensorId": "",
  "annotationId": "",
  "description": "Metadata labeler",
  "values": [
    {
      "instances": [
        {
          "instanceId": "2",
          "transformRecord": {
            "rotationEuler": [ 0.0, 180.0, 0.0 ],
            "position": [ 0.0, 1.0, -4.0 ],
            "localScale": [ 1.0, 1.0, 1.0 ],
            ...
          }
        },
        ...
      ]
    }
  ]
}
```

### Included Metadata Tags

| MetadataTag                               | Information Collected                                          |
|-------------------------------------------|----------------------------------------------------------------|
| `LabelingNameMetadataTag`                 | GameObject's name                                              |
| `LabelingTagMetadataTag`                  | GameObject's tag.                                              |
| `LightMetadataTag`                        | Intensity and Color of the attached Light                      |
| `LabelingChildNameMetadataTag`            | GameObject's name but added as information under chosen parent |
| `LabelingDistanceToMainCameraMetadataTag` | Distance between the GameObject and the Main Camera            |
| `LabelingKeyValuesMetadataTag`            | List of custom string data attached to the GameObject          |
| `LabelingTransformDataMetadataTag`        | GameObject's transform (rotation, localPosition, etc.)         |

## Custom Metadata Tags

Metadata can either be related to a labeled object in the scene (i.e. to a `Labeling` component via its unique instance id) or it can just be an environment-related metadata i.e. not tied to an instance ids. Environment-related and Labeling-related metadata are put into different sections in the output json metric.

### Environment-related Custom Metadata Tag

Environment-related custom metadata tags can inherit from the `BaseMetadataTag` class. For example, to create a custom Metadata tag that records the number of enabled and disabled lights in the scene, we would have the following custom class.

```csharp
public class LightCountReporterTag : BaseMetadataReportTag
{
    protected override string key => "light_count";
 
    protected override void GetReportedValues(IMessageBuilder builder)
    {
        var lights = FindObjectsOfType<Light>();
        uint enabledLightCount = 0;
        uint disbledLightCount = 0;
        
        foreach(var light in lights) {
            if (light.enabled)
                enabledLightCount += 1;
            else
                disbledLightCount += 1;
        }
    
        builder.AddUInt("enabled_light_count", enabledLightCount);
        builder.AddUInt("disabled_light_count", disabledLightCount);
    }
}
```

We can then add this component to any GameObject as it is environment-related metadata and doesn't depend on the GameObject it is attached to. Running a simulation would produce the following json metric.


```json
{
  "@type": "type.unity.com/unity.solo.GenericMetric",
  "id": "metadata",
  "sensorId": "",
  "annotationId": "",
  "description": "Metadata labeler",
  "values": [
    {
      "light_count": {
        "enabled_light_count": 23,
        "disabled_light_count": 3
      }
    }
  ]
}
```

### Labeling-related Custom Metadata Tag

Labeling-related custom metadata tags should inherit from the `LabelMetadataTag` class - a thin wrapper on top of the `BaseMetadataTag` which automatically associates the reported metadata with the instance id of the `Labeling` component it is attached to. For example, to create a custom metadata tag that records which scene that GameObject is a part of, we can have the following class:

```csharp
public LabelingSceneNameTag: LabelMetadataTag {

    protected override string key => "in_scene";
 
    protected override void GetReportedValues(IMessageBuilder builder)
    {
         builder.Add("scene_name", gameObject.scene.name);
         builder.Add("scene_path", gameObject.scene.path);
    }
}
```

Once added to a GameObject with a `Labeling` component, we can run the simulation to get the following output json metric.

```json
{
  "@type": "type.unity.com/unity.solo.GenericMetric",
  "id": "metadata",
  "sensorId": "",
  "annotationId": "",
  "description": "Metadata labeler",
  "values": [
    {
      "instances": [
        {
          "instanceId": "9",
          // key of the custom tag
          "in_scene": {
            "scene_name": "SampleScene",
            "scene_path": "Assets/Scenes/SampleScene.unity"
          }
        }
      ]
    }
  ]
}
```
