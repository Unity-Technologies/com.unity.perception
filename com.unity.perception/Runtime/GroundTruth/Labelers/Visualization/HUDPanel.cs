using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Heads up display panel used to publish a key value pair on the screen. Items added to this need
    /// to have their values updated every frame, or else, they will be determined to be stale and removed
    /// from the view and re-used for a new entry.
    /// </summary>
    public class HUDPanel : MonoBehaviour
    {
        readonly Dictionary<string, Dictionary<string, string>> entries = new Dictionary<string, Dictionary<string, string>>();

        private GUIStyle keyStyle;
        private GUIStyle valueStyle;

        private const int LineHeight = 22;
        private const int XPadding = 10;
        private const int YPadding = 10;
        private const int BoxWidth = 200;
        private const int YLineSpacing = 4;

        private void Awake()
        {
            keyStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5),
                normal = {textColor = Color.white}
            };

            valueStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(10, 10, 5, 5),
                normal = {textColor = Color.white}
            };
        }

        /// <summary>
        /// Updates (or creates) an entry with the passed in key value pair
        /// </summary>
        /// <param name="labeler">The labeler that requested the HUD entry</param>
        /// <param name="key">The key of the HUD entry</param>
        /// <param name="value">The value of the entry</param>
        public void UpdateEntry(CameraLabeler labeler, string key, string value)
        {
            var name = labeler.GetType().Name;
            if (!entries.ContainsKey(name)) entries[name] = new Dictionary<string, string>();
            entries[name][key] = value;
        }

        private Vector2 scrollPosition;

        private bool guiStylesInitialized = false;

        private void SetUpGUIStyles()
        {
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.font = Resources.Load<Font>("Inter-Light");
            GUI.skin.label.padding = new RectOffset(0, 0, 1, 1);
            GUI.skin.label.margin = new RectOffset(0, 0, 1, 1);
            GUI.skin.box.padding = new RectOffset(5, 5, 5, 5);
            GUI.skin.toggle.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.horizontalSlider.margin = new RectOffset(0, 0, 0, 0);
            guiStylesInitialized = true;
        }

        int EntriesCount()
        {
            return entries.Count + entries.Sum(entry => entry.Value.Count);
        }

        internal void onDrawGUI()
        {
            if (entries.Count == 0) return;

            if (!guiStylesInitialized) SetUpGUIStyles();

            GUI.depth = 0; // Draw HUD objects on the top of other UI objects

            var height = Math.Min(LineHeight * EntriesCount(), Screen.height * 0.5f - YPadding * 2);
            var xPos = Screen.width - BoxWidth - XPadding;
            var yPos = Screen.height - height - YPadding;

            GUILayout.BeginArea(new Rect(xPos, yPos, BoxWidth, height), GUI.skin.box);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            bool firstTime = true;
            foreach (var labeler in entries.Keys)
            {
                if (!firstTime) GUILayout.Space(YLineSpacing);
                firstTime = false;
                GUILayout.Label(labeler);
                foreach (var entry in entries[labeler])
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(5);
                    GUILayout.Label(entry.Key);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(entry.Value);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Removes the key value pair from the HUD
        /// </summary>
        /// <param name="labeler">The labeler that requested the removal</param>
        /// <param name="key">The key of the entry to remove</param>
        public void RemoveEntry(CameraLabeler labeler, string key)
        {
            if (entries.ContainsKey(labeler.GetType().Name))
            {
                entries[labeler.GetType().Name].Remove(key);
            }
        }

        /// <summary>
        /// Removes all of the passed in entries from the HUD
        /// </summary>
        /// <param name="labeler">The labeler that requested the removal</param>
        public void RemoveEntries(CameraLabeler labeler)
        {
            entries.Remove(labeler.GetType().Name);
        }
    }
}
