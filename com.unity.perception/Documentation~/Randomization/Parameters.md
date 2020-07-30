# Parameters

All parameters are derived from one of three abstract classes:
1. Categorical parameters
2. Typed parameters
3. Struct parameters

### Categorical Parameters

Categorical parameters by definition choose a value from a list of options that have no intrinsic ordering. For example, a material paramater randomly chooses from a list of material options, but the list of material options itself can be rearranged into any particular order without affecting the distribution of materials selected.

If your custom parameter is a categorical in nature, the [StringParameter]() class included in the perception package can be used as a reference when deriving the `CategoricalParameter` class:

**Note:** the AddComponentMenu attribute with an empty string prevents parameters from appearing in the Add Component GameObject menu. Randomization parameters should only be created with by a `ParameterConfiguration`

### Typed Parameters

Typed parameters are the most generic form of parameter, often using samplers to allow a user to choose different probability distributions to control how samples are generated. To implement a typed parameter, derive the TypedParameter class and implement the Sample() and Samples() methods.

### Struct Parameters

Typed parameters often use a struct instead of a class for their parameter type. If this is the case, you can derive the more specific `StructParameter` class to implement the JobHandle overload of the Samples() method for increased sampling performance. Take a look at the [ColorHsvaParameter]() class included in the perception package for an example on how to implement a struct parameter.

## Performance
Using the JobHandle overload of the Samples() method is recommended to increase sampling performance when generating large numbers of samples. The JobHandle Samples() overload will utilize the Unity Burst Compiler to optimize the operations used to compute new samples and the Unity Job System to automatically multithread the samplers employed across multiple parameters. Below is an example of sampling two parameters (and their samplers) in parallel to generate random color and position samples:
```
// Schedule sampling jobs
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
