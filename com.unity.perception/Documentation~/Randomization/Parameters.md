# Parameters

All parameters are derived from one of three abstract classes:
1. Categorical parameters
2. Struct parameters
3. Typed parameters

### Categorical Parameters

Categorical parameters by definition choose a value from a list of options that have no intrinsic ordering. For example, a material paramater randomly chooses from a list of material options, but the list of material options itself can be rearranged into any particular order without affecting the distribution of materials selected.

If your custom parameter is a categorical in nature, take a look at the [StringParameter]() class included in the perception package as a reference for how to derive the `CategoricalParameter` class:
```
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("String")]
    public class StringParameter : CategoricalParameter<string> {}
}
```

**Note:** the AddComponentMenu attribute with an empty string prevents parameters from appearing in the Add Component GameObject menu. Randomization parameters should only be created with by a `ParameterConfiguration`

### Struct Parameters

If the intended output type of a parameter is a struct instead of a class, deriving the `StructParameter` class will create new parameter with access to the JobHandle overload of the Samples() method for increased sampling performance. Take a look at the [ColorHsvaParameter]() class included in the perception package for an example on how to implement a struct parameter.

### Typed Parameters

Typed parameters are the most generic form of parameter. To implement a typed parameter, derive the TypedParameter class and implement the Sample() and Samples() methods.

## Performance
It is recommended to use the JobHandle overload of the Samples() method when generating a large number of samples. The JobHandle overload will utilize the Unity Burst Compiler and Job System to automatically optimize and multithread parameter sampling jobs. The code block below is an example of how to use this overload to sample two parameters in parallel:
```
// Schedule sampling jobs
var currentIteration = ScenarioBase.ActiveScenario.currentIteration
var cubeColors = ObjectColor.Samples(currentIteration, constants.objectCount, out var colorHandle);
var cubePositions = ObjectPosition.Samples(currentIteration, constants.objectCount, out var positionHandle);

// Combine job handles
var handles = JobHandle.CombineDependencies(colorHandle, positionHandle);

// Wait for the jobs to complete
handles.Complete();

// Use the created samples
for (var i = 0; i < constants.objectCount; i++)
{
    m_ObjectMaterials[i].SetColor(k_BaseColorProperty, cubeColors[i]);
    m_Objects[i].transform.position = cubePositions[i];
}

// Dispose of the generated samples
cubeColors.Dispose();
cubePositions.Dispose();
```
