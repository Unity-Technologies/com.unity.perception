# Labeling
Many labelers require mapping the objects in the view to the values recorded in the dataset. As an example, Semantic Segmentation needs to determine the color to draw each object in the segmentation image.

This mapping is accomplished for a GameObject by:
* Finding the nearest Labeling component attached to the object or its parents.

* Finding the first label in the Labeling component that is present anywhere in the Labeler's Label Config.

Unity uses the resolved Label Entry from the Label Config to produce the final output.

## Labeling component
The Labeling component associates a list of string-based labels with a GameObject and its descendants. A Labeling component on a descendant overrides its parent's labels.

### Limitations
Labeling is supported on MeshRenderers, SkinnedMeshRenderers, and partially supported on Terrains.

On terrains, the labels will be applied to the entire terrain. Trees and details can not be labeled. They will always render as black or zero in instance and segmentation images and will occlude other objects in ground truth.

## Label Config
Many labelers require a Label Config asset. This asset specifies a list of all labels to be captured in the dataset along with extra information used by the various labelers.

## Best practices
Generally algorithm testing and training requires a single label on an asset for proper identification such as "chair", "table" or "door". To maximize asset reuse, however, it is useful to give each object multiple labels in a hierarchy.

For example, you could label an asset representing a box of Rice Krispies as `food\cereal\kellogs\ricekrispies`

* "food": type
* "cereal": subtype
* "kellogs": main descriptor
* "ricekrispies": sub descriptor

If the goal of the algorithm is to identify all objects in a Scene that are "food", that label is available and can be used. Conversely if the goal is to identify only Rice Krispies cereal within a Scene that label is also available. Depending on the goal of the algorithm, you can use any mix of labels in the hierarchy.

