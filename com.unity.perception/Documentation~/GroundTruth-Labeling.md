_Note: This document is a work in progress_

# Labeling
Accurately labeling assets with a predefined taxonomy will inform training and testing of algorithms as to which objects in a dataset have importance. Example: assets labeled with “table” and “chair” will provide an algorithm with the information it needs to train on identifying these objects separately within a scene.


You can add a Labeling component to individual GameModels within a scene although it is a good practice to create a prefab of a GameModel and apply the Labeling component to it.
The Labeling components contain properties that control the number of labels applied to the GameModel. “Classes” has a property named “size”, this identifies how many labels are applied to a GameModel. Default = 0 (no label). Setting “size” to 1 will expose an “Element 0” parameter and an input field allowing for a custom label as text or numbers (combination of both) that can be used to label the asset.

Multiple labels can be used by setting “size” to 2 or more. These additional Elements (labels) can be used for any purpose in development. For example in SynthDet labels have a hierarchy where Element0 is the highest level label identifying an GameModel in a very general category. Subsequent categories become more focused in identifying what types and groups an object can be classified. The last Element is reserved for the specific name (or label) the asset is defined as.

# Labeling Configuration
Semantic segmentation (and other metrics) require a labeling configuration file located here: 
This file gives a list of all labels currently being used in the data set and what RGB value they are associated with. This file can be used as is or created by the developer. When a Semantic segmentation output is generated the per pixel RGB value can be used to identify the object for the algorithm.

Note: the labeling configuration file is not validated and must be managed by the developer.

## Best practices
Generally algorithm testing and training requires a single label on an asset for proper identification (“chair”, “table”, “door, “window”, etc.) In Unity SynthDet a labeling hierarchy is used to identify assets at a higher level and/or more granularly.

Example
An asset representing a box of Rice Krispies cereal is labeled as: food\cereal\kellogs\ricekrispies
“food” - type
“cereal” - subtype
“kellogs” - main descriptor
“ricekrispies” - sub descriptor

If the goal of the algorithm is to identify all objects in a scene that is “food” that label is available and can be used. Conversely if the goal is to identify only Rice Krispies cereal within a scene that label is also available. Depending on the goal of the algorithm any mix of labels in the hierarchy can be used at the discretion of the developer.

Note: this labeling hierarchy is suggested and not required. Please adjust or discard if your project goals have other requirements.
Adding Labels to a Unity Asset
Labels are added to Unity Assets by attaching a labeling script to an asset and creating a prefab object.

### Asset Organization
