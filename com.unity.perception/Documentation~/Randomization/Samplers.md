# Samplers
Samplers in the perception package are classes that deterministically generate random float values from bounded probability distributions. Although samplers are often used in conjunction with parameters to generate arrays of typed random values, samplers can be instantiated and used from any ordinary script:
```
var sampler = new NormalSampler();
sampler.seed = 123456789u;
sampler.mean = 3;
sampler.stdDev = 2;
sampler.range = new FloatRange(-10, 10);

// Generate a sample
var sample = sampler.NextSample();
```

Four Samplers are included with the perception package:
1. Constant Sampler
2. Uniform Sampler
3. Normal Sampler 
4. Placeholder Range Sampler

#### Constant Sampler
Generates constant valued samples

#### Uniform Sampler
Samples uniformly from a specified range

#### Normal Sampler
Generates random samples from a truncated normal distribution bounded by a specified range

#### Placeholder Range Sampler
Used to define a float range [minimum, maximum] for a particular component of a parameter (example: the hue component of a color parameter). This sampler is useful for configuring sample ranges for non-perception related scripts, particularly when these scripts have a public interface for manipulating a minimum and maximum bounds for their sample range but perform the actual sampling logic internally.


## Performance

Samplers are designed to be Unity Burst Compiler and Job System compatible to increase simulation performance when generating large numbers of samples. Below is an example of a simple job that uses a NormalSampler directly to create 100 normally distributed samples:
```
[BurstCompile]
public struct SampleJob : IJob
{
    NormalSampler sampler;
    public NativeArray<float> samples;
    
    public void Execute()
    {
        for (var i = 0; i < samples.Length; i++)
            samples[i] = sampler.NextSample();
    }
}
```
Additionally, samplers have a NativeSamples() method that can schedule a ready-made multi-threaded job intended for generating a large array of samples. Below is an example of how to combine two job handles returned by NativeSamples() to generate two arrays of samples simultaneously:
```
// Create samplers
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


## Custom Samplers
Take a look at the [UniformSampler](../../Runtime/Randomization/Samplers/SamplerTypes/UniformSampler) and [NormalSampler](../../Runtime/Randomization/Samplers/SamplerTypes/NormalSampler) structs as references for implementing your own [ISampler](../../Runtime/Randomization/Samplers/ISampler). Note that the NativeSamples() method in the ISampler interface requires the usage of the Unity Job System. Take a look [here](https://docs.unity3d.com/Manual/JobSystem.html) to learn more about how to create jobs using the Unity Job System.
