# Randomizer Tags

RandomizerTags are the primary mechanism by which Randomizers query for a certain subset of GameObjects to randomize within a simulation.

More specifically, RandomizerTags are components that can be added to GameObjects to register them with the active Scenario's TagManager. This TagManager is aware of all objects with tags in the scene and can be queried to find all GameObjects that contain a specific tag. Below is a simple example of a ColorRandomizer querying for all GameObjects with a ColorRandomizerTag that it will apply a random material base color to:

```
[Serializable]
[AddRandomizerMenu("Perception/Color Randomizer")]
public class ColorRandomizer : Randomizer
{
    static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor");

    public ColorHsvaParameter colorParameter;

    protected override void OnIterationStart()
    {
        var taggedObjects = tagManager.Query<ColorRandomizerTag>();
        foreach (var taggedObject in taggedObjects)
        {
            var renderer = taggedObject.GetComponent<MeshRenderer>();
            renderer.material.SetColor(k_BaseColor, colorParameter.Sample());
        }
    }
}
```

RandomizerTags can also be used to customize how Randomizers apply their randomizations to a particular GameObject. Visit [Phase 2 of the Perception Tutorial](../Tutorial/TUTORIAL.md) to explore an in depth example of implementing a LightRandomizer that does exactly this.

