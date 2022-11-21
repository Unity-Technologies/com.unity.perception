# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## Unreleased

### Upgrade Notes

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Known Issues

## [1.0.0-preview.1] - 2022-11-17

### Upgrade Notes

The Perception package has been upgraded to support versions 2021.3+ and 2022.1+ of the Unity Editor. Along side this change, support for Unity Editor version 2020 has been deprecated. Existing Unity 2020 projects that use the perception package will need to be upgraded to ensure compatibility with the Perception package.

CategoricalParameter type specialization classes (e.g. GameObjectParameter) have been deprecated and will need to be replaced with their template equivalent (e.g. CategoricalParameter<GameObject>).

### Added

Added the `timestamp` field back to the JSON output for captures.

Added the Accumulation feature for rendering techniques that require accumulated frames, with the primary use case being path tracing.

The keypoint labeler can now be configured to show an icon for an occluded keypoint during real-time visualization.

Sample scenes for different labelers

Added full SkinnedMeshRenderer support to the 3D Bounding Box labeler, enabling tight-fitting boxes on humans, animals, and other animated characters.

Added Metadata labeler.

Multiple semantic segmentation labelers can be present at once

RenderedObjectInfo and bounding box labelers now reports hierarchical relationships (such as instance id of parent, child, etc.) for all rendered objects.

Added support for IEnumerable in IMessageBuilder.

Super resolution option to the Unity Camera Sensor

Added a new "Sensor Type" field to the PerceptionCamera to enable users to author, select, and capture data from different camera sensor types and projections within the perception framework.

Added label id and label names for each object reported in an Instance Segmentation annotation.

Added each keypoint's cartesian location in camera space

Editor And Runtime Settings added. This can be used to transfer configs from editor to runtime

Dataset generation can be resumed

Perception endpoint will now serialize annotations that implement the IMessageProvider interface.

Added consumer endpoint validation to check if endpoint properties are valid. Consumer endpoint now calls an endpoint's validation and displays errors to the user.

Added alphaThreshold parameter the PerceptionCamera to configure how translucent materials appear in segmentation images.

Added layerMaskOverride parameter to the PerceptionCamera to configure object filtering during labeler data capture.

Added a check to warn users if multiple GroundTruthRenderFeatures are present in URP projects.

### Changed

Changed the default serialization format from perception to Solo.

The Perception package has been upgraded to support versions 2021.3.7+ and 2022.1+ of the Unity Editor.

Support for Unity Editor version 2020 has been deprecated.

Renamed "super resolution" Perception Camera feature to "super sampling" to clarify the exact technique used to generate the anti-aliasing effect.

Authoring new forms of pixel based ground truth data has been simplified with the new CameraSensorChannel interface. A single CameraSensorChannel will now do the work of what originally took three coordinated custom pass classes (URP, HDRP, and CrossPipeline) to accomplish.

The type specializations of the generic CategoricalParameter class can now be used directly. Derived classes like GameObjectParameter are no longer required to define new type specializations for the CategoricalParameter class.

Deprecated the CategoricalParameter.GetCategoryCount() method and replaced it with the new CategoricalParameter.Count property

Semantic segmentation annotations report labels present in the image

### Removed

Dataset visualizer removed

URP support removed 

Removed redundant type specialization classes that derive from the  CategoricalParameter class (e.g. GameObjectParameter).

### Fixed

Fixed a bug where frames would not be written to disk until the simulation was finished or quit.

Fixed a bug where using the crash restore feature would cause the `--output-path` flag to be ignored.

Fixed the name of the description field in the sensor definition JSON output. Changed from `definition` field `description`.

Moved `Frames Per Iteration` field into the `Scenario Properties` pane in the scenario editor. 

Depth Labeler now reports what measurement strategy it's using to capture depth values.

Fixed banding artifacts in darker parts of captured images when super resolution is enabled.

Perception version data provider in case of no Resources folder in the project.

Keypoint visualizations are now a consistent size regardless of screen resolution.

Fixed a UI bug with the Perception Camera's "Override Layer Mask" toggle that caused an exception to be thrown when toggling the option.

Fixed the empty spacing between items in the Categorical Paremeter UI.

Metadata labaler delayed frame output generation.

Fixed burst compilation error with EncodeJob<T>()

Fixed job thread count being set to zero in PixelWeightsUtility when the job system is disabled

Fixed sporadic pixel position labeler test failures due to a shader that was not prewarmed

Issue with keypoint labelers and GameObjects without Animator components

Metrics now report empty strings instead of null.

GenericMetrics now report vector of values instead of just the first value.

Renamed RgbCapture imageEncodingFormat to imageFormat to match Solo spec

Fix FloatRangeElement uxml so that it doesn't cause errors in Unity versions over 2021

Exception in KeypointLabeler when deleting and recreating humans each iteration

The PerceptionCamera component with no labelers will no longer throw NullReferenceExceptions when viewed in the inspector.

AnimationRandomizer now chooses random normalized start time for the selected animation instead of always starting from the beginning.

### Known Issues

When using the Accumulation feature, the Scene view should not be visible at runtime, otherwise the captured images will not be accumulated.

## [0.11.1-preview.1] - 2022-04-13

### Added

Added the ability to define the output of a standalone player with an '--output-path' command line argument

### Changed

Exceptions thrown in randomizers will now end the run

Duplicates in categorical parameters are checked and reported

User output paths are now written out in user's settings

### Fixed

Fixed captured RGB images being upside down or dark in various cases

Fixed UI when there are many randomizers in the project

Fixed Scenario UI when the last randomizer is removed

Fixed the Visualizer installation bug, Visualizer can be installed and opened properly now.

## [0.10.0-preview.1] - 2022-03-09

### Upgrade Notes

### Known Issues

### Added

Added support for consumer endpoints allowing a user to define how they want to generated data to be serialized.

Added perception project settings to allow for different perception settings to be modified, including the selection of an active endpoint.

Added progress bar to the Fixed Length Scenario.

Added the ability to drag and drop items directly onto the UI for Categorical Parameters.

Added required `labelerId` field for all CameraLabelers.

Added additional generic metrics for bool, bool array, and vector3.

An image file format can now be selected for RGB capture, the instance segmentation labeler, and the semantic segmentation labeler.

### Changed

Moved FixedLengthScenario.Constants.framesPerIteration into FixedLengthScenario so that it is only controlled by the application.

Changed the dataset capture package to use consumer endpoints instead of only serializing to the perception format.

Changed dataset capture to report data on a per frame basis.

Upgraded com.unity.collections package dependency version to a non-preview release.

Removed dependency on com.unity.simulation packages.

Changed FixedFrameScenario to no longer be cloud specific.

### Deprecated

### Removed

Removed com.unity.simulation.capture package dependency.

Removed com.unity.simulation.core and related dependencies.

Removed Run in Unity Simulation window.

### Fixed

Handled exception from analytics when labeler had an empty label config.

Updated documentation to reflect that the 3D Bounding Box Labeler does not support Skinned Mesh Renderers.

PoissonDiskSampling will no longer generate empty lists of points for editor versions 2020.3.27f1 and above.

Fixed perception endpoint to support multiple cameras.

Fixed potential race condition with annotations being registered from within asynchronous lambda jobs. Image related annotations will now be registered on the main thread in callbacks executed after ImageEncoder jobs have completed.

The PerceptionCamera UI now shows a warning when a missing/null labeler is detected.


## [0.9.0-preview.2] - 2021-11-01

### Upgrade Notes

Scenario JSON configurations that were generated using previous versions will need to be manually updated due to updates to serialization and deserialization of scenarios.

### Known Issues

### Added

Added support for multiple cameras capturing simultaneously (visualization not yet supported)

Users can now delay the current iteration for one frame from within randomizers by calling the `DelayIteration` function of the active scenario.

Users can now choose the base folder location to store their generated datasets.

Added new configuration options to the Scenario JSON configuration. These include a `limits` block on numerical Scalars and Samplers to denote a valid range, and a `state` block on the Randomizers for enabling/disabling them from the config.

Added the AssetSource class for loading assets from generic sources inside randomizers.

Added a `projection` field in the capture.sensor metadata. Values are either "perspective" or "orthographic".

Added usage analytics for the scenario configuration. For more information, refer to the Release Notes for package version 0.9.0.preview.2.

### Changed

Upgraded capture package dependency to 0.0.10-preview.24. This fixes an issue where RGB images were blank on OSX when MSAA was enabled & post-processing was disabled.

Changed the JSON serialization key of Normal Sampler's standard deviation property from "standardDeviation" to "stddev".

Changed the JSON serialization of Randomizers in a scenario to an array of groups rather than a dictionary.

### Deprecated

### Removed

### Fixed

Fixed an indexing issue with the IdLabelConfig editor. When a new label was added to an empty Id Label Config with Auto Assign IDs enabled, the starting id (0 or 1) was ignored and the new label would always have an id of 0.

## [0.8.0-preview.4] - 2021-07-05

### Upgrade Notes

### Known Issues

When using URP in OSX, having MSAA enabled on the camera while the post-processing option is disabled may cause the output RGB images to be blank. As a workaround, you can disable MSAA and use FXAA instead, until the issue is fixed.

### Added

Added support for 'step' button in editor.

Added random seed field to the Run in Unity Simulation Window.

Added support for keypoint self occlusion.

Added the ability to adjust keypoint self occlusion tolerance per keypoint in keypoint template file.

Added the ability to adjust keypoint self occlusion tolerance on a user defined joint using the joint label.

Added a Keypoint Occlusion Override component which allows a user to universally scale all of the keypoint tolerances for a model.

Make IdLabelConfig.GetAnnotationSpecification() public.

### Changed

Increased color variety in instance segmentation images.

The PoissonDiskSampling utility now samples a larger region of points to then crop to size of the intended region to prevent edge case bias.

Upgraded capture package dependency to 0.0.10-preview.23 to fix two issues: (1) Post processing effects were not included when capturing images in URP (2) RGB images were upside-down when post processing effects were enabled and FXAA disabled.

### Deprecated

### Removed

### Fixed

Fixed keypoint labeling bug when visualizations are disabled.

Fixed an issue where Simulation Delta Time values larger than 100 seconds in Perception Camera would cause incorrect capture scheduling behavior.

Fixed an issue where Categorical Parameters sometimes tried to fetch items at `i = categories.Count`, which caused an exception.

## [0.8.0-preview.3] - 2021-03-24

### Changed

Expanded documentation on the Keypoint Labeler
Updated Keypoint Labeler logic to only report keypoints for visible objects by default
Increased color variety in instance segmentation images

### Fixed

Fixed compiler warnings in projects with HDRP on 2020.1 and later

Fixed a bug in the Normal Sampler where it would return values less than the passed in minimum value, or greater than the passed in maximum value, for random values very close to 0 or 1 respectively.


## [0.8.0-preview.2] - 2021-03-15

### Upgrade Notes

All appearances of the term `KeyPoint` have been renamed to `Keypoint`. If you have code that relies on any renamed types or names, make sure to alter your code to reflect the new names.

`ScenarioBase`'s `Awake()`, `Start()`, and `Update()` functions are now private. If you previously used these, replace the usages with `OnAwake()`, `OnStart()`, and `OnUpdate()`.

The interface `IGroundTruthGenerator` now contains a new method named `ClearMaterialProperties` for disabling ground truth generation on a `Labeling` component or its associated `MaterialPropertyBlock`. Update your implementing classes to including this method.

### Known Issues

### Added

Added error message when missing Randomizer scripts are detected.

Scenario serialization has been updated to include scalar values on Randomizers and Parameters.

Added new `ScenarioBase` virtual lifecycle hooks: `OnAwake()`, `OnStart()`, `OnUpdate()`, `OnComplete()`, and `OnIdle()`.

Keypoint occlusion has been added. No keypoint information will be recorded for a labeled asset completely out of the camera's frustum. 

New keypoint tests have been added to test keypoint states.

The color of keypoints and connections are now reported in the annotation definition JSON file for keypoint templates.

The `PerceptionScenario` abstract class has been added to abstract perception data capture specific functionality from the vanilla Scenario lifecycle. 

The newly added `LabelManager` class now enables custom Labelers to access the list of registered `Labeling` Components present in the Scene.

Improved UI for `KeypointTemplate` and added useful default colors for keypoint and skeleton definitions.

Added the ability to switch ground truth generation on or off for an object at runtime by enabling or disabling its `Labeling` component. A new method named `ClearMaterialProperties()` in `IGroundTruthGenerator` handles this functionality.

### Changed

Renamed all appearances of the term `KeyPoint` within types and names to `Keypoint`.

ScenarioBase's `Awake()`, `Start()`, and `Update()` methods are now private. The newly added virtual lifecycle hooks are to be used as replacements.

Improved _Run in Unity Simulation_ window UI.

The _Run in Unity Simulation_ window now accepts an optional Scenario JSON configuration to override existing Scenario editor UI settings.

The `GetRandomizer()` and `CreateRandomizer()` methods of `ScenarioBase` have been augmented or replaced with more generic list index style accessors.

The Scenario inspector buttons for serialization and deserialization have been refactored to open a file explorer so that the user can choose where to save the generated JSON configuration or which file to import a configuration from.

RandomizerTags now use `OnEnable()` and `OnDisable()` to manage their lifecycle. This allows the user to toggle them on and off in the editor.

Upgraded `com.unity.simulation.capture` package dependency to integrate new changes that prevent the API updater from looping infinitely when opening the project settings window on new URP projects.

`CameraLabeler` methods `OnBeginRendering()` and `OnEndRendering()` now have an added `ScriptableRenderContext` parameter.

### Deprecated

The Randomizer methods `OnCreate()`, `OnStartRunning()`, and `OnStopRunning()` are now deprecated and have been replaced with `OnAwake()`, `OnEnable()` and `OnDisable()` respectively, so as to better reflect the existing MonoBehaviour lifecycle methods.

### Removed

Removed the Entities package dependency.

### Fixed

Fixed a null reference error that appeared when adding options to Categorical Parameters.

Fixed ground truth not properly being produced when there are other disabled PerceptionCameras present. Note: this does not yet add support for multiple enabled PerceptionCameras.

Fixed an exception when rendering inspector for Randomizers with private serialized fields.

Fixed an issue preventing the user from adding more options to a Categorical Parameter's list of options with the _Add Folder_ button. _Add Folder_ now correctly appends the contents of the new folder to the existing list.

Fixed a bug where uniform probabilities were not properly reset upon adding or removing options from a Categorical Parameter's list of options.

Fixed keypoints being reported in wrong locations on the first frame in which an object is visible. 

Fixed an out of range error that occurred when a keypoint template skeleton relied on a joint that was not available.

Fixed wrong labels on 2d bounding boxes when all labeled objects are deleted in a frame.

## [0.7.0-preview.2] - 2021-02-08

### Upgrade Notes

### Known Issues

### Added

Added Register() and Unregister() methods to the RandomizerTag API so users can implement RandomizerTag compatible GameObject caching

### Changed

Switched accessibility of scenario MonoBehaviour lifecycle functions (Awake, Start, Update) from private to protected to enable users to define their own overrides when deriving the Scenario class.

The GameObjectOneWayCache has been made public for users to cache GameObjects within their own custom Randomizers.

### Deprecated

### Removed

### Fixed

Fixed the math offsetting the iteration index of each Unity Simulation instance directly after they deserialize their app-params.

The RandomizerTagManager now uses a LinkedHashSet data structure to register tags to preserve insertion order determinism in Unity Simulation.

GameObjectOneWayCache now correctly registers and unregisters RandomizerTags on cached GameObjects.

## [0.7.0-preview.1] - 2021-02-01

### Upgrade Notes

#### Randomization Namespace Change
The Randomization toolset has been moved out of the Experimental namespace. After upgrading to this version of the Perception package, please follow these steps:
* Replace all references to `UnityEngine.Experimental.Perception.Randomization` with `UnityEngine.Perception.Randomization` in your C# code.
* Open your Unity Scene file in a text editor and replace all mentions of `UnityEngine.Experimental.Perception.Randomization` with `UnityEngine.Perception.Randomization`, and save the file.

#### Random Seed Generation
Replace usages of `ScenarioBase.GenerateRandomSeed()` with `SamplerState.NextRandomState()` in your custom Randomizer code.

#### Sampler Ranges 
Before upgrading a project to this version of the Perception package, make sure to keep a record of **all sampler ranges** in your added Randomizers. Due to a change in how sampler ranges are serialized, **after upgrading to this version, ranges for all stock Perception samplers (Uniform and Normal Samplers) will be reset**, and will need to be manually reverted by the user.

#### Tag Querying
The `RandomizerTagManager.Query<T>` function now returns the tag object itself instead of the GameObject it is attached to. You will need to slightly modify your custom Randomizers to accommodate this change. Please refer to the included sample Randomizers as examples.

### Known Issues

The bounding box 3D labeler does not work with labeled assets that utilize a skinned mesh renderer. These are commonly used with animated models.

### Added

Added keypoint ground truth labeling

Added animation randomization

Added ScenarioConstants base class for all scenario constants objects

Added ScenarioBase.SerializeToConfigFile()

Randomizer tags now support inheritance

Added AnimationCurveSampler, which returns random values according to a range and probability distribution denoted by a user provided AnimationCurve. 

Added ParameterUIElementsEditor class to allow custom ScriptableObjects and MonoBehaviours to render Parameter and Sampler typed public fields correctly in their inspector windows.

Added new capture options to Perception Camera:
* Can now render intermediate frames between captures.
* Capture can now be triggered manually using a function call, instead of automatic capturing on a schedule.

Added 3D bounding box visualizer

Categorical Parameters will now validate that their specified options are unique at runtime.

### Changed

Randomizers now access their parent scenario through the static activeScenario property.

Unique seeds per Sampler have been replaced with one global random seed configured via the ScenarioConstants of a Scenario

Samplers now derive their random state from the static SamplerState class instead of individual scenarios to allow parameters and samplers to be used outside of the context of a scenario

Replaced ScenarioBase.GenerateRandomSeed() with SamplerState.NextRandomState() and SamplerState.CreateGenerator()

ScenarioBase.Serialize() now directly returns the serialized scenario configuration JSON string instead of writing directly to a file (use SerializeToConfigFile() instead)

ScenarioBase.Serialize() now not only serializes scenario constants, but also all sampler member fields on randomizers attached to the scenario

RandomizerTagManager.Query<T>() now returns RandomizerTags directly instead of the GameObjects attached to said tags

Semantic Segmentation Labeler now places data in folders with randomized filenames.

The uniform toggle on Categorical Parameters will now reset the Parameter's probability weights to be uniform.

Reorganized Perception MonoBehaviour paths within the AddComponentMenu.

Upgraded the Unity Simulation Capture package dependency to 0.0.10-preview.18 and Unity Simulation Core to 0.0.10-preview.22

### Deprecated

### Removed

Removed ScenarioBase.GenerateRandomSeedFromIndex()

Removed native sampling (through jobs) capability from all samplers and parameters as it introduced additional complexity to the code and was not a common usage pattern

Removed `range` as a required ISampler interface property.

Removed randomization tooling from the "Experimental" namespace

### Fixed

Fixed an issue where the overlay panel would display a full screen semi-transparent image over the entire screen when the overlay panel is disabled in the UI

Fixed a bug in instance segmentation labeler that erroneously logged that object ID 255 was not supported

Fixed the simulation stopping while the editor/player is not focused

Fixed memory leak or crash occurring at the end of long simulations when using BackgroundObjectPlacementRandomizer or ForegroundObjectPlacementRandomizer

Randomizer.OnCreate() is no longer called in edit-mode when adding a randomizer to a scenario

Fixed a bug where removing all randomizers from a scenario caused the randomizer container UI element to overflow over the end of Scenario component UI

Semantic Segmentation Labeler now produces output in the proper form for distributed data generation on Unity Simulation by placing output in randomized directory names

Texture Randomizer is now compatible with HDRP.

Categorical Parameters no longer produce errors when deleting items from long options lists.

Parameter, ISampler, and non-generic Sampler class UIs now render properly in MonoBehaviours and ScriptableObjects.

Fixed an issue in the perception tutorial sample assets where upon the editor being first opened, and a user generates a dataset by clicking the play button, the first generated image has duplicated textures and hue offsets for all background objects. Enabling the "GPU instancing" boolean in the tutorial's sample material's inspector fixed this issue.

## [0.6.0-preview.1] - 2020-12-03

### Added

Added support for labeling Terrain objects. Trees and details are not labeled but will occlude other objects.
Added analytics for Unity Simulation runs

Added instance segmentation labeler.

Added support for full screen visual overlays and overlay manager.

All-new editor interface for the Labeling component and Label Configuration assets. The new UI improves upon various parts of the label specification and configuration workflow, making it more efficient and less error-prone to setup a new Perception project.

Added Assets->Perception menu for current and future asset preparation and validation tools. Currently contains one function which lets the user create prefabs out of multiple selected models with one click, removing the need for going through all models individually.

### Changed

Updated dependencies to com.unity.simulation.capture:0.0.10-preview.14, com.unity.simulation.core:0.0.10-preview.20, and com.unity.burst:1.3.9.

Changed InstanceSegmentationImageReadback event to provide a NativeArray\<Color32\> instead of NativeArray\<uint\>.

Expanded all Unity Simulation references from USim to Unity Simulation.

Uniform and Normal samplers now serialize their random seeds.

The ScenarioBase's GenerateIterativeRandomSeed() method has been renamed to GenerateRandomSeedFromIndex().

### Deprecated

### Removed

### Fixed

UnitySimulationScenario now correctly deserializes app-params before offsetting the current scenario iteration when executing on Unity Simulation.

Fixed Unity Simulation nodes generating one extra empty image before generating their share of the randomization scenario iterations.

Fixed enumeration in the CategoricalParameter.categories property.

The GenerateRandomSeedFromIndex method now correctly hashes the current scenario iteration into the random seed it generates.

Corrupted .meta files have been rebuilt and replaced.

The Randomizer list inspector UI now updates appropriately when a user clicks undo.
    

## [0.5.0-preview.1] - 2020-10-14

### Known Issues

Creating a new 2020.1.x project and adding the perception package to the project causes a memory error that is a [known issue in 2020.1 editors](https://issuetracker.unity3d.com/issues/wild-memory-leaks-leading-to-stackallocator-walkallocations-crashes). Users can remedy this issue by closing and reopening the editor.

### Added

Added Randomizers and RandomizerTags
Added support for generating 3D bounding box ground truth data

### Changed

### Deprecated

### Removed

Removed ParameterConfigurations (replaced with Randomizers)

### Fixed

Fixed visualization issue where object count and pixel count labelers were shown stale values
Fixed visualization issue where HUD entry labels could be too long and take up the entire panel

## [0.4.0-preview.1] - 2020-08-07

### Added

Added new experimental randomization tools

Added support for 2020.1

Added Labeling.RefreshLabeling(), which can be used to update ground truth generators after the list of labels or the renderers is changed

Added support for renderers with MaterialPropertyBlocks assigned to individual materials

### Changed

Changed the way realtime visualizers rendered to avoid rendering conflicts

Changed default labeler ids to be lower-case to be consistent with the ids in the dataset

Switched to latest versions of com.unity.simulation.core and com.unity.simulation.capture

### Deprecated

### Removed

### Fixed

Fixed 2d bounding boxes being reported for objects that do not match the label config.

Fixed a categorical parameter UI error in which deleting an individual option would successfully remove the option from the UI but only serialize the option to null during serialization instead of removing it

Fixed the "Application Frequency" parameter UI field not initializing to a default value

Fixed the IterateSeed() method where certain combinations of indices and random seeds would produce a random state value of zero, causing Unity.Mathematics.Random to throw an exception

Fixed labeler editor to allow for editing multiple labelers at a time

Fixed labeler editor to ensure that when duplicating prefabs all labeler entries are also duplicated

Fixed colors in semantic segmentation images being darker than those specified in the label config

Fixed objects being incorrectly labeled when they do not match any entries in the label config

Fixed lens distortion in URP and HDRP now being applied to ground truth

### Security

## [0.3.0-preview.1] - 2020-08-07

### Added

Added realtime visualization capability to the perception package.

Added visualizers for built-in labelers: Semantic Segmentation, 2D Bounding Boxes, Object Count, and Rendered Object Info.

Added references to example projects in manual.

Added notification when an HDRP project is in Deferred Only mode, which is not supported by the labelers.

### Changed

Updated to com.unity.simulation.capture version 0.0.10-preview.10 and com.unity.simulation.core version 0.0.10-preview.17

Changed minimum Unity Editor version to 2019.4

### Fixed

Fixed compilation warnings with latest com.unity.simulation.core package.

Fixed errors in example script when exiting play mode

## [0.2.0-preview.2] - 2020-07-15

### Fixed

Fixed bug that prevented RGB captures to be written out to disk
Fixed compatibility with com.unity.simulation.capture@0.0.10-preview.8

## [0.2.0-preview.1] - 2020-07-02

### Added

Added CameraLabeler, an extensible base type for all forms of dataset output from a camera.
Added LabelConfig\<T\>, a base class for mapping labels to data used by a labeler. There are two new derived types - ID label config and semantic segmentation label config.

### Changed

Moved the various forms of ground truth from PerceptionCamera into various subclasses of CameraLabeler.
Renamed SimulationManager to DatasetCapture.
Changed Semantic Segmentation to take a SemanticSegmentationLabelConfig, which maps labels to color pixel values.

## [0.1.0] - 2020-06-24

### This is the first release of the _Perception_ package
