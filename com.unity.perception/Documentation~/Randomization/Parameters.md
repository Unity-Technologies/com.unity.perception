# Parameters

## Lookup Parameters

To obtain a parameter from a paramter configuration, use the GetParameter() method:
```
// Get a reference to the parameter configuration attached to this GameObject
var parameterConfiguration = GetComponent<ParameterConfiguration>();

// Lookup the parameter "ObjectWidth" by name
var parameter = GetComponent<FloatParameter>("ObjectWidth");
``` 

## Creating and Sampling Parameters

Parameters are typically managed by `ParameterConfigurations` in the Unity Editor. However, parameters can be instanced independently like a regular class too:
```
// Create a color parameter
var colorParameter = new HsvaColorParameter();

// Generate one color sample
var color = colorParameter.Sample();
```

Note that parameters, like samplers, generate new random values for each call to the Sample() method:
```
var color1 = colorParameter.Sample();
var color2 = colorParameter.Sample();
Assert.AreNotEqual(color1, color2);
```

## Defining Custom Parameters

All parameters derive from the `Parameter` abstract class, but all included perception package parameter types derive from two specialized Parameter base classes:
1. `CategoricalParameter`
2. `NumericParameter`

### Categorical Parameters

Categorical parameters choose a value from a list of options that have no intrinsic ordering. For example, a material paramater randomly chooses from a list of material options, but the list of material options itself can be rearranged into any particular order without affecting the distribution of materials selected.

If your custom parameter is a categorical in nature, take a look at the [StringParameter]() class included in the perception package as a reference for how to derive the `CategoricalParameter` class.
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

### Numeric Parameters

Numeric parameters use samplers to generate randomized structs. Take a look at the [ColorHsvaParameter]() class included in the perception package for an example on how to implement a numeric parameter.

## Improving Sampling Performance
For numeric parameters, it is recommended to use the JobHandle overload of the Samples() method when generating a large number of samples. The JobHandle overload will utilize the Unity Burst Compiler and Job System to automatically optimize and multithread parameter sampling jobs. The code block below is an example of how to use this overload to sample two parameters in parallel:
```
// Get a reference to the parameter configuration attached to this GameObject
var parameterConfiguration = GetComponent<ParameterConfiguration>();

// Lookup parameters
var cubeColorParameter = parameterConfiguration.GetParameter<HsvaColorParameter>("CubeColor");
var cubePositionParameter = parameterConfiguration.GetParameter<Vector3Parameter>("CubePosition");

// Schedule sampling jobs
var cubeColors = cubeColorParameter.Samples(constants.cubeCount, out var colorHandle);
var cubePositions = cubePositionParameter.Samples(constants.cubeCount, out var positionHandle);

// Combine job handles
var handles = JobHandle.CombineDependencies(colorHandle, positionHandle);

// Wait for the jobs to complete
handles.Complete();

// Use the created samples
for (var i = 0; i < constants.cubeCount; i++)
{
    m_ObjectMaterials[i].SetColor(k_BaseColorProperty, cubeColors[i]);
    m_Objects[i].transform.position = cubePositions[i];
}

// Dispose of the generated samples
cubeColors.Dispose();
cubePositions.Dispose();
```
