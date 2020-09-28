# Perception Tutorial
## Phase 2: Custom Randomizations

In Phase 1 of the tutorial, we learned how to use the Randomizers that are bundled with the Perception package to spawn background and foreground objects, and randomize their position, rotation, texture, and hue offset (color). In this phase, we will build a custom light Randomizer for the `Directional Light` object, affecting the light's intensity and color on each `Iteration`. We will also learn how to include certain data or logic inside a randomized object (such as the light) in order to more explicitly define and restrict its randomization behaviors.

We need to create two C# classes for our light randomization, `LightRandomizer` and `LightRandomizerTag`. The first of these will sample random values and assign them to various aspects of the light, and the second class will be the component that will be added to `Directional Light`, making it a target of `LightRandomizer`.

* **Action**: In the _**Project**_ tab, right-click on the `Scripts` folder and select _**Create -> C# Script**_. Name your new script file `LightRandomizer.cs`.
* **Action**: Create another script and name it `LightRandomizerTag.cs`.
* **Action**: Double-click `LightRandomizer.cs` to open it in _**Visual Studio**_.

Note that while _**Visual Studio**_ is the default option, you can choose any C# compatible editor of your choice. You can change the default settings in _**Preferences -> External Tools -> External Script Editor**_.

* **Action**: Remove the contents of the class and copy/paste the code below:

```
using System;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;

[Serializable]
public class LightRandomizer : Randomizer
{
    public FloatParameter lightIntensityParameter;

    protected override void OnIterationStart()
    {
        var taggedObjects = tagManager.Query<LightRandomizerTag>();

        foreach (var taggedObject in taggedObjects)
        {
            var light = taggedObject.GetComponent<Light>();
            if (light)
            {
                light.intensity = lightIntensityParameter.Sample();
            }
        }
    }
}
```

The purpose of this piece of code is to obtain a random float parameter and assign it to the light's `Intensity` field. Let's go through the code above and understand each part. The `FloatParameter` field makes it possible for us to define a randomized float parameter and modify its properties within the UI, similar to how we already modified the properties for the previous Randomizers we used. 

If you return to your list of Randomizers in the _**Inspector**_ view of `SimulationScenario`, you can now add this new Randomizer.

* **Action**: Add `LightRandomizer` to the list of Randomizers in `SimulationScenario`.

You will notice the the Randomizer's UI snippet contains one Parameter named `Light Intensity Parameter`. This is the same Parameter we added in the code block above. Here, you can set the sampling distribution (`Value`), `Seed`, and `Range` for this float Parameter:

<p align="center">
<img src="Images/light_rand_1.png" width="420"/>
</p>


* **Action**:  In the UI snippet for `LightRandomzier`, set range minimum and maximum to 0.5 and 3.

The `LightRandomizer` class extends `Randomizer`, which is the base class for all Randomizers that can be added to a `Scenario`. This base class provides a plethora of useful functions and properties that can help catalyze the process of creating new Randomziers.

The `OnIterationStart()` function is used for telling the Randomizer what actions to perform at the start of each `Iteration` of the `Scenario`. As seen in the code block, in each `Iteration` this class queries the `tagManager` object for all objects that carry the `LightRandomizerTag` component. Then, for each object inside the queried list, it first tries to get the `Light` component, and if this component exists, the next line sets its intensity to a new random float sampled from `lightIntensityParamter`. 

Note that the `if (light)` is not a requirement if you make sure to only add the `LightRandomizerTag` component to objects that have a `Light` component; however, it is good practice to guard against possible mistakes by always make sure a component exists and is not null before using it.

* **Action**: Open `LightRandomizerTag.cs` and replace its contents with the code below:

```
using UnityEngine.Experimental.Perception.Randomization.Randomizers;

public class LightRandomizerTag : RandomizerTag
{    
}
```

Yes, the Randomizer tags can be this simple if you just need to attach objects to Randomizers. Later, you will learn how to add code here to encapsulate more data and logic within the randomized objects. 
* **Action**: Select `Directional Light` in the Scene's _**Hierarchy**_, and in the _**Inspector**_ tab, add a `Light Randomizer Tag` component.
* **Action**: Run the simulation again and inspect how `Directional Light` now switches between different intensities. You can pause the simulation and then use the step button (to the right of the pause button) to move the simulation one frame forward and clearly see the varying light intensity

Let's now add more variation to our light by randomizing its color as well. 

* **Action**: Define a new `ColorRgbParameter`:

`public ColorRgbParameter lightColorParameter`

* **Action**: Inside the code block that intensity was previously applied, add code for sampling color from the above Parameter and applying it:

```
if (light)
{
    light.intensity = lightIntensityParameter.Sample();
    light.color = lightColorParameter.Sample();
}
```            

If you now check the UI snippet for `LightRandomizer`, you will notice that `Color Parameter` is now added. This Parameter includes four separate values for `Red`, `Green`, `Blue` and `Alpha`. The range for these is 0 to 1, which is already set and does not need to be modified. However, in order to get varied colors and not just different shades of grey, we need to set different seeds for each value. 

