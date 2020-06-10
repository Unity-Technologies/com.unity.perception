# Labeling
Accurately labeling assets with a predefined taxonomy will inform training and testing of algorithms as to which objects in a dataset have importance. Example: assets labeled with “table” and “chair” will provide an algorithm with the information it needs to train on identifying these objects separately within a scene.

You can add a Labeling component to individual GameObjects within a scene although it is a good practice to create a prefab of a GameModel and apply the Labeling component to it.

Multiple labels can be assigned to the same `Labeling`. When ground truth which requires unique labels per object is being generated, the first label in the `Labeling` present anywhere in the `LabelingConfiguration` is used.

## Labeling Configuration
Many labelers require require a `Labeling Configuration` asset.
This file specifies a list of all labels to be captured in the dataset for a labeler along with extra information used by the various labelers.

## Best practices
Generally algorithm testing and training requires a single label on an asset for proper identification such as “chair”, “table”, or “door". To maximize asset reuse, however, it is useful to give each object multiple labels in a hierarchy.

For example
An asset representing a box of Rice Krispies cereal could be labeled as `food\cereal\kellogs\ricekrispies`

* “food” - type
* “cereal” - subtype
* “kellogs” - main descriptor
* “ricekrispies” - sub descriptor

If the goal of the algorithm is to identify all objects in a scene that is “food” that label is available and can be used. Conversely if the goal is to identify only Rice Krispies cereal within a scene that label is also available. Depending on the goal of the algorithm any mix of labels in the hierarchy can be used.