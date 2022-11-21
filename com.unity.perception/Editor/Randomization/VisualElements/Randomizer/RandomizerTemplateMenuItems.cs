using UnityEngine;

namespace UnityEditor.Perception.Randomization
{
    static class RandomizerTemplateMenuItems
    {
        internal static readonly string s_PlacementTemplatePath = $"Packages/com.unity.perception/Editor/Randomization/Templates/PlacementRandomizer.template";
        internal static readonly string s_RandomizerTagTemplatePath = $"Packages/com.unity.perception/Editor/Randomization/Templates/RandomizerTag.template";

        [MenuItem("Assets/Create/Perception/C# Randomizer and RandomizerTag")]
        static void MenuCreateRandomizerCSharpScript()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(s_RandomizerTagTemplatePath, $"NewRandomizerTag.cs");
        }

        [MenuItem("Assets/Create/Perception/C# Simple Placement Randomizer")]
        static void MenuCreatePlacementRandomizerCSharpScript()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(s_PlacementTemplatePath, "NewPlacementRandomizer.cs");
        }
    }
}
