# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## Unreleased

### Upgrade Notes

All appearances of the term `KeyPoint` have been renamed to `Keypoint`. Therefore, after upgrading to this version, if you have code that relies on any renamed types or names, make sure to alter your code to reflect the new names.

### Known Issues

### Added

Scenario serialization has been updated to include scalar values on randomizers and parameters.

Added new ScenarioBase virtual lifecycle hooks: OnAwake, OnStart, OnComplete, and OnIdle.

### Changed

Renamed all appearances of the term `KeyPoint` within types and names to `Keypoint`.

ScenarioBase's Awake, Start, and Update methods are now private. The newly added virtual lifecycle hooks are to be used as replacements.

Improved Run Unity Simulation window UI.

The Run Unity Simulation window now accepts option scenario JSON configurations to override existing scenario editor settings.

ScenarioBase's Get and Create randomizer methods have been augmented or replaced with more generic list index style accessors.

The scenario inspector buttons serialize and deserialize have been refactored to open a file explorer generate and import JSON configurations.

Randomizer tags now use OnEnable and OnDisable to manage lifecycle. This allows the user to toggle them on and off in the editor.

### Deprecated

### Removed

### Fixed

Fixed a null reference error that appeared when adding options to categorical parameters.

Fixed ground truth not properly produced when there are other disabled PerceptionCameras present. Note: this does not yet add support for multiple enabled PerceptionCameras.

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
