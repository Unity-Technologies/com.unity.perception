# Randomizer Library

A brief overview of currently added randomizers can be seen below.
Each randomizer component is accessible at `Library/{Randomizer Name}` and the corresponding tags at `Library Tags/{Randomizer Name}Tag`.
### Note: Randomizer vs Tags
Randomizers are added to the scenario while tags are added to objects in the scene.
Often, a tag is used to specify information specific to the GameObject such that it can be randomized uniquely with respect to other objects.

For example, the `LightRandomizer` added to the Scenario does not contain any configurable parameters.
On the other hand, the `LightRandomizerTag` contains configurable properties such as the range of Intensity to the randomize the light between, range of colors, etc.
This allows different Lights (and thus different `LightRandomizerTag`'s) to be randomized differently – some can have green and blue colors, some red and pink, some bright, some dim, etc.

### 1. Light Randomizer
**Light Randomizer**: Targets all objects with an attached `Light RandomizerTag`  
**Light Randomizer Tag**: Attached to a GameObject with a Light component.

Independently specifies the following:
1. Probability of the Light to be Off
2. Light Intensity
3. Light Temperature
4. Light Color

### 2. Material Property Randomizer
**Material Property Randomizer**: Targets all objects with an attached `Material Property Randomizer Tag`  
**Material Property Randomizer Tag**: Attached to a GameObject with some kind of Renderer (MeshRenderer, BillboardRenderer, etc.)

For the Shader of the selected Material, specifies which shader properties to modify and between what ranges.

### 3. Material Swapper
**Material Swapper**: Targets all objects with an attached `Material Swapper Tag`  
**Material Swapper Tag**: Attached to a GameObject with a Material.

Given a list of Materials, Swaps the material of the given GameObject each iteration with one sampled from the list.

### 4. Scene Randomizer
**Scene Randomizer**: Given a list of scenes, loads a scene from the list every `n` iterations.

**Note: None of the scenes in the list should have a Scenario component as only one Scenario component can be active at one time. The randomizers specified in the Scenario of the starting component will persist and act on the objects in the newly loaded scene.**

### 5. Skybox Randomizer
**Skybox Randomizer**: Targets all objects with an attached `Skybox Randomizer Tag`.  
**Skybox Randomizer Tag**: Attached to a GameObject with a Volume component.

The Skybox (and rotation of the skybox) of the attached Volume is randomized each iteration based on the configuration of the `Skybox Randomizer`.

### 6. Substance Randomizer
**Note: Only available when the `SUBSTANCE_PLUGIN_ENABLED` compile flag is specified. This should be automatically added when the Adobe Substance 3D plugin is installed. However, the flag may persist and need to be removed manually when the plugin is removed.**

**Substance Randomizer**: Targets all objects with an attached `Substance Randomizer Tag`.  
**Substance Randomizer Tag**: Attached to a GameObject with a SubstanceGraph component.

For the selected Substance Graph, randomizes the substance properties every `n` iterations according to the ranges specified internally/implicitly in the Substance Graph.
The iteration interval for randomization (`n`) is specified in the `Substance Randomizer`.

**Warning: Substance Graph randomization is unstable and may lead to crashes! (especially when running on the CV Datasets)**

### 7. Transform Randomizer
**Transform Randomizer**: Targets all objects with an attached `Transform Randomizer Tag`  
**Transform Randomizer Tag**: Attached to any GameObject.

Independently specifies the following:
1. Translation
    1. Enabled (whether to randomize position?)
    2. Range of Translation
    3. Whether translation is Relative (Offset from starting position) or Absolute (Global position values)
2. Rotation
    1. Enabled (whether to randomize rotation?)
    2. Range of Rotation
    3. Whether rotation is Relative (Offset from starting rotation) or Absolute (Global euler angles)
3. Scale
    1. Enabled (whether to randomize scale?)
    2. Range of Scaling
    3. Uniformly scaled? (Whether all axes have the same randomized value)
    4. Whether rotation is Relative (multiplied from starting scale) or Absolute (Global scale values)

### 8. Volume Randomizer
**Volume Randomizer**: Targets all objects with an attached `Volume Randomizer Tag`.  
**Volume Randomizer Tag**: Attached to a GameObject with a Volume component.

From a list of available post-processing effects, which effects to randomize and the particular parameters of each post-processing effect.

#### Supported Post-Processing Effects and their Configurable Parameters
1. **Bloom** – Threshold, Intensity, Scatter
2. **Exposure** – Compensation
3. **Depth of Field** – Near Focus Start & End, Far Focus Start & End
4. **Camera Type** – List of Camera Specs (Focal Length, Sensor Size, Lens Shift, Gate Fit, etc.)
5. **Motion Blur** – Intensity, Minimum Velocity, Maximum Velocity
6. **Lens Distortion** – Intensity, X & Y Multiplier, Center, Scale
