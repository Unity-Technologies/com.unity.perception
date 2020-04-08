# Perception

The Perception package contains tools for authoring and executing autonomous vehicle simulations. They are organized into two categories: Scenarios and Sensors.

## Scenarios

TODO

# Setup for local development
* Clone the perception repository into an arbirary directory on disk
* Install and use Unity 2019.3.0b7

## Option 1: PerceptionHDRP/PerceptionURP
The repository includes two projects for local development in `TestProjects`, one set up for HDRP and the other for URP.

## Option 2: Set up a project from scratch
*The following instructions reference the Unity doc's page on [installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html)*
* Create a new HDRP project or open an existing one
* Open your project's `<project root>/Packages/manifest.json` in a text editor
* At the end of the file, add `"registry": "https://artifactory.prd.cds.internal.unity3d.com/artifactory/api/npm/upm-candidates"`
    * _Note: This step will be removed once the dependency `com.unity.entities-0.2.0-preview.*` is published publically._
* Back in Unity, open the Package Manager window
* Add the High Definition RP package, version 7.1.2 or later
* Click the ***+*** button in the upper lefthand corner of the window
* Click the ***add package from disk*** option
* Select to the package.json file under the com.unity.perception folder in your cloned perception repository
* To allow the compilation and running of tests, add `"testables": [ "com.unity.perception" ]`
    * For an example `manifest.json`, see `TestProjects/PerceptionTest/Packages/manifest.json`
    * For more on the `manifest.json` schema, see the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.7/manual/index.html#advanced-package-topics)

## Suggested IDE Setup
For closest standards conformity and best experience overall, JetBrains Rider or Visual Studio w/ JetBrains Resharper are suggested. For optimal experience, perform the following additional steps:
* To allow navigating to code in all packages included in your project, in your Unity Editor, navigate to `Edit -> Preferences... -> External Tools` and check `Generate all .csproj files.` 
* To get automatic feedback and fixups on formatting and naming convention violations, set up Rider/JetBrains with our Unity standard .dotsettings file by following [these instructions](https://github.cds.internal.unity3d.com/unity/com.unity.coding/tree/master/UnityCoding/Packages/com.unity.coding/Coding~/Configs/JetBrains).
* If you use VS Code, install the Editorconfig extension to get automatic code formatting according to our conventions.