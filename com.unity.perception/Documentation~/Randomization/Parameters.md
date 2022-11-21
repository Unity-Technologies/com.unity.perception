# Parameters

## Creating and Sampling Parameters

Parameters are often defined as fields of a Randomizer class, but they can also be instanced just like any other C# class:
```
// Create a color Parameter
var colorParameter = new HsvaColorParameter();

// Generate one color sample
var color = colorParameter.Sample();
```

Note that Parameters, like Samplers, generate new random values for each call to the Sample() method:
```
var color1 = colorParameter.Sample();
var color2 = colorParameter.Sample();
Assert.AreNotEqual(color1, color2);
```

## Defining Custom Parameters

All Parameters derive from the `Parameter` abstract class. Additionally, the Parameters included in the Perception package  types derive from two specialized Parameter base classes:
1. `CategoricalParameter`
2. `NumericParameter`

## Using Parameters outside of Randomizers (ie: in MonoBehaviours and ScriptableObjects)

After adding a public Parameter field to a MonoBehaviour or ScriptableObject, you may have noticed that the Parameter's UI does not look the same as it does when added to a Randomizer. This is because the Inspector UI for most Perception randomization components is authored using Unity's relatively new UI Elements framework, though by default, Unity uses the old IMGUI framework to render default inspector editors.

Say you have the following CustomMonoBehaviour that has a public GameObjectParameter field:
```csharp
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

public class CustomMonoBehaviour : MonoBehaviour
{
    public GameObjectParameter prefabs;
}
```

To force Unity to use UI Elements to render your CustomMonoBehaviour's inspector window, create a custom editor for your MonoBehaviour by deriving the ParameterUIElementsEditor class like so:

```csharp
using UnityEditor;
using UnityEngine.Perception.Editor;

[CustomEditor(typeof(CustomMonoBehaviour))]
public class CustomMonoBehaviourEditor : ParameterUIElementsEditor { }
``` 

**_Note_**: Any editor scripts must be placed inside an "Editor" folder within your project. "Editor" is a [special folder name](https://docs.unity3d.com/Manual/SpecialFolders.html) in Unity that prevents editor code from compiling into a player during the build process. For example, the file path for the CustomMonoBehaviourEditor script above could be ".../Assets/Scripts/Editor/CustomMonoBehaviourEditor".

### Categorical Parameters

Categorical Parameters choose a value from a list of options that have no intrinsic ordering. For example, a material Parameter randomly chooses from a list of material options, but the list of material options itself can be rearranged into any particular order without affecting the distribution of materials selected.

**Note:** the AddComponentMenu attribute with an empty string prevents Parameters from appearing in the Add Component GameObject menu. Randomization Parameters should only be created with by a `ParameterConfiguration`

### Numeric Parameters

Numeric Parameters use samplers to generate randomized structs. Take a look at the [ColorHsvaParameter]() class included in the Perception package for an example on how to implement a numeric Parameter.
