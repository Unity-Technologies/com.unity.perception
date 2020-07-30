# Samplers
Samplers in the perception package are classes that deterministically generate random float values from bounded probability distributions. Although samplers are often used in conjunction with parameters to generate arrays of typed random values, samplers can be instantiated and used from any ordinary script:
```
var currentScenarioIteration = ParameterConfiguration.ActiveConfig.scenario.currentIteration;
var sampler = new NormalSampler();
sampler.seed = 91239871u;
sampler.mean = 3;
sampler.stdDev = 2;
sampler.range = new FloatRange(-10, 10);
var sample = sampler.Sample(currentScenarioIteration);
```

Four Samplers are included with the perception package:
1. Uniform Sampler
2. Normal Sampler
3. Constant Sampler
4. Placeholder Range Sampler

#### Uniform Sampler
Samples uniformly from a specified range

#### Normal Sampler
Generates random samples from a truncated normal distribution bounded by a specified range

#### Constant Sampler
Generates constant valued samples

#### Placeholder Range Sampler
Used to define a float range [minimum, maximum] for a particular component of a parameter (example: the hue component of a color parameter). This sampler is useful for configuring sample ranges for non-perception related scripts, particularly when these scripts have a public interface for manipulating a minimum and maximum bounds for their sample range but perform the actual sampling logic internally.


## Performance
Using the JobHandle overload of the Samples() method is recommended to increase sampling performance when generating large numbers of samples. The JobHandle Samples() overload will utilize the Unity Burst Compiler to optimize the operations used to compute new samples and the Unity Job System to automatically multithread the samplers employed across multiple parameters. Below is an example of sampling two samplers in parallel to generate an array of uniform and an array of normal float samples.
```
// Create samplers
var uniformSampler = new UniformSampler();
var normalSampler = new NormalSampler();

// Create sample jobs
var currentScenarioIteration = ParameterConfiguration.ActiveConfig.scenario.currentIteration;
var uniformSamples = uniformSampler.Samples(currentScenarioIteration, 1000, out var uniformHandle);
var normalSamples = normalSampler.Samples(currentScenarioIteration, 1000, out var normalHandle);

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


## Custom Samplers
To implement your own custom probability distributions and samplers, derive the [RandomSampler]() abstract class and implement the Sample() and Samples() methods. Take a look at the [UniformSampler]() and [NormalSampler]() classes as references for constructing your own samplers. Note that the JobHandle overload of the Samples() method in the RandomSampler abstract class uses the Unity Job System to improve sampling performance. Take a look [here](https://docs.unity3d.com/Manual/JobSystem.html) to learn more about improving sampling performance using the Unity Job System.
