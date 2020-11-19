using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor.Perception.AssetPreparation
{
    public class CreatePrefabFromSelectedAssets : MonoBehaviour
    {
        /// <summary>
        /// Function for creating prefabs from multiple models with one click. Created prefabs will be placed in the same folder as their corresponding model.
        /// </summary>
        [MenuItem("Assets/Perception/Create Prefabs from Selected Models")]
        static void CreatePrefabsFromSelectedModels()
        {
            foreach (var selection in Selection.gameObjects)
            {
                var path = AssetDatabase.GetAssetPath(selection);
                var tmpGameObject = Instantiate(selection);
                var destinationPath = Path.GetDirectoryName(path) + "/" + selection.name + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(tmpGameObject, destinationPath);
                DestroyImmediate(tmpGameObject);
            }
        }
    }

}
