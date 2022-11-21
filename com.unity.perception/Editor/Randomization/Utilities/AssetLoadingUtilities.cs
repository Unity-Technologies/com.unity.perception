using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Perception.Randomization
{
    static class AssetLoadingUtilities
    {
        public static List<Object> LoadAssetsFromFolder(string folderPath, Type assetType)
        {
            if (!folderPath.StartsWith(Application.dataPath))
                throw new ApplicationException("Selected folder is not an asset folder in this project");
            var assetsPath = "Assets" + folderPath.Remove(0, Application.dataPath.Length);
            var assetIds = AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { assetsPath });
            var assets = new List<Object>();
            foreach (var guid in assetIds)
                assets.Add(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), assetType));
            return assets;
        }
    }
}
