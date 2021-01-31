# Samplers
Samplers in the perception package are classes that deterministically generate random float values from bounded probability distributions. Although Samplers are often used in conjunction with Parameters to generate arrays of typed random values, Samplers can be instantiated and used from any ordinary script:
```
var sampler = new NormalSampler();
sampler.mean = 3;
sampler.stdDev = 2;
sampler.range = new FloatRange(-10, 10);

// Generate a sample
var sample = sampler.NextSample();
```

Three Samplers are included with the perception package:
1. Constant Sampler
2. Uniform Sampler
3. Normal Sampler 

#### Constant Sampler
Generates constant valued samples

#### Uniform Sampler
Samples uniformly from a specified range

#### Normal Sampler
Generates random samples from a truncated normal distribution bounded by a specified range


## Random Seeding
Samplers generate random values that are seeded by the active Scenario's current random state. Changing the Scenario's random seed will result in Samplers generating different values. Changing the order of Samplers, Parameters, or Randomizers will also result in different values being sampled during a simulation.

It is recommended that users do not generate random values using the UnityEngine.Random() class or the System.Random() class within a simulation since both of these classes can potentially generate non-deterministic or improperly seeded random values. Using only Perception Samplers to generate random values will help ensure that Perception simulations generate consistent results during local execution and on Unity Simulation in the cloud.


## Custom Samplers
Take a look at the [UniformSampler](../../Runtime/Randomization/Samplers/SamplerTypes/UniformSampler) and [NormalSampler](../../Runtime/Randomization/Samplers/SamplerTypes/NormalSampler) structs as references for implementing your own [ISampler](../../Runtime/Randomization/Samplers/ISampler). Note that the NativeSamples() method in the ISampler interface requires the usage of the Unity Job System. Take a look [here](https://docs.unity3d.com/Manual/JobSystem.html) to learn more about how to create jobs using the Unity Job System.


## Performance

Samplers have a NativeSamples() method that can schedule a ready-made multi-threaded job intended for generating a large array of samples. Below is an example of how to combine two job handles returned by NativeSamples() to generate two arrays of samples simultaneously:
```
// Create Samplers
var uniformSampler = new UniformSampler
{ 
    range = new FloatRange(0, 1),
    seed = 123456789u
};
var normalSampler = new NormalSampler
{
    range = new FloatRange(0, 1),
    mean = 0,
    stdDev = 1,
    seed = 987654321u
};

// Create sample jobs
var uniformSamples = uniformSampler.NativeSamples(1000, out var uniformHandle);
var normalSamples = normalSampler.NativeSamples(1000, out var normalHandle);

// Combine job handles
var combinedJobHandles = JobHandle.CombineDependencies(uniformHandle, normalHandle);

// Wait for jobs to complete
combinedJobHandles.Complete();

//...
// Use samples
//...

// Dispose of sample arrays
uniformSamples.Dispose();
normalSamples.Dispose();
```
