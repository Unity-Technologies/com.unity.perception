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
  <summary><strong>Q:</strong> How can I add new labels to objects at runtime?</summary>

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

  Labeling works on the GameObject level, so to achieve the scenarios described here, you will need to break down your main object into multiple GameObjects parented to the same root object, and assign `Labeling` components to each of the inner objects, as shown below.

<p align="center">
<img src="images/inner_objects.png" width="500"/>
</p>
  
  Alternatively, in cases where parts of the surface of the object need to be labeled, you can add labeled invisible surfaces on top of these sections. To create a transparent surface
  
  ```C#
  gameObject.GetComponent<Labeling>().enabled = false;  
  ```
</details>

## Randomization

## Perception Camera

## Capture and Dataset Format

## Miscellaneous

