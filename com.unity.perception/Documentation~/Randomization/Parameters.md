# Parameters

## Creating and Sampling Parameters

Parameters are often defined as fields of a randomizer class, but they can also be instanced just like any other C# class:
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