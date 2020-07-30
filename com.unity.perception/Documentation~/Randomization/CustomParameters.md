# Custom Parameters

All parameters are derived from one of three abstract classes:
1. Categorical parameters
2. Typed parameters
3. Struct parameters

## Categorical Parameters

Categorical parameters by definition choose a value from a list of options that have no intrinsic ordering. For example, a material paramater randomly chooses from a list of material options, but the list of material options itself can be rearranged into any particular order without affecting the distribution of materials selected.

If your custom parameter is a categorical in nature, the [StringParameter]() class included in the perception package can be used as a reference when deriving the `CategoricalParameter` class:

**Note:** the AddComponentMenu attribute with an empty string prevents parameters from appearing in the Add Component GameObject menu. Randomization parameters should only be created with by a `ParameterConfiguration`

## Typed Parameters

Typed parameters are the most generic form of parameter, often using samplers to allow a user to choose different probability distributions to control how samples are generated. To implement a typed parameter, derive the TypedParameter class and implement the Sample() and Samples() methods.

## Struct Parameters

Typed parameters often use a struct instead of a class for their parameter type. If this is the case, you can derive the more specific `StructParameter` class to implement the jobified NativeArray Samples() method for increased sampling performance. Take a look at the [ColorHsvaParameter]() class included in the perception package for an example on how to implement a struct parameter.
