using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;

[Serializable]
[AddRandomizerMenu("Perception/PrefabRandomizer")]
public class PrefabRandomizer : Randomizer
{
    public CategoricalParameter<GameObject> prefabs = new();

    protected override void OnIterationStart()
    {
        // Disable all active prefabs.
        for (var i = 0; i < prefabs.Count; i++)
        {
            var b = prefabs.GetCategory(i);
            b.SetActive(false);
        }

        // Enable a random prefab.
        prefabs.Sample().SetActive(true);
    }
}
