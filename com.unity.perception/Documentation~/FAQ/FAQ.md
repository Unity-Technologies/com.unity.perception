# FAQ and Code Samples

This page covers a variety of topics, including common questions and issues that may arise while using the Perception package, as well as code samples and recommendations for a number of popular workflows and use-cases.

## Labeling





<details>
  <summary><strong>Q:</strong> How can I disable or enable labeling on an object at runtime?</summary>

  **A**: You can turn labeling on and off on a GameObject by switching the enabled state of its `Labeling` component. For example:
  
  ```C#
  gameObject.GetComponent<Labeling>().enabled = false;  
  ```
</details>

<details>
  <summary><strong>Q:</strong> How can I remove or add new labels to objects at runtime?</summary>

This can be achieved through modifying the `labels` list of the `Labeling` component. The key is to call `RefreshLabeling` on the component after making any changes to the labels. Example:

```C#
var labeling = gameObject.GetComponent<Labeling>();
labeling.labels.Clear();
labeling.labels.Add("new-label");
labeling.RefreshLabeling();
```
Keep in mind that any new label added with this method should already be present in the `LabelConfig` attached to the `Labeler` that is supposed to label this object.
</details>

<details>
  <summary><strong>Q:</strong> Is it possible to label only parts of an object or assign different labels to different parts of objects?</summary>

  Labeling works on the GameObject level, so to achieve the scenarios described here, you will need to break down your main object into multiple GameObjects parented to the same root object, and add `Labeling` components to each of the inner objects, as shown below.

<p align="center">
<img src="images/inner_objects.png" width="800"/>
</p>
  
  Alternatively, in cases where parts of the surface of the object need to be labeled (e.g. decals on objects), you can add labeled invisible surfaces on top of these sections. These invisible surfaces need to have a fully transparent material. To create an invisible material:

  * Create a new material (***Assets -> Create -> Material***) and name it `TransparentMaterial`
  * Set the `Surface Type` for the material to `Transparent`, and set the alpha channel of the `Base Map` color to 0.
    * For HDRP: In addition to the above, disable `Preserve specular lighting` 
   
  An example labeled output for an object with separate labels on inner objects is shown below:

<p align="center">
<img src="images/inner_labels.gif" width="600"/>
</p> 
</details>

<details>
  <summary><strong>Q:</strong> 

When visible surfaces of two objects are fully aligned, the bounding boxes seem to blink in and out of existence from one frame to another. Why is that?</summary>

This is due to a common graphics problem called *z-fighting*. This occurs when the shader can't decide which of the two surfaces to draw on top of the other, since they both have the exact same distance from the camera. To fix this, simply move one of the objects slightly so that the two problematic surfaces do not fully align.

</details>


## Randomization

<details>
  <summary><strong>Q:</strong> 

I need to randomize the rotation of my objects in such a way that they always face the camera, but rotate in other directions. How can I achieve that with a Randomizer?</summary>



</details>


## Perception Camera

## Capture and Dataset Format

## Miscellaneous

