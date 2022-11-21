using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Heads up display panel used to publish a key value pair on the screen. Items added to this need
    /// to have their values updated every frame, or else, they will be determined to be stale and removed
    /// from the view and re-used for a new entry.
    /// </summary>
    public class HUDPanel : MonoBehaviour
    {
        readonly Dictionary<CameraLabeler, Dictionary<string, string>> m_Entries = new Dictionary<CameraLabeler, Dictionary<string, string>>();

        GUIStyle m_KeyStyle;
        GUIStyle m_ValueStyle;

        const int k_LineHeight = 22;
        const int k_XPadding = 10;
        const int k_YPadding = 10;
        const int k_BoxWidth = 200;
        const int k_YLineSpacing = 4;
        const int k_MaxKeyLength = 20;

        /// <summary>
        /// The number of labelers currently displaying real-time information on the visualization HUD
        /// </summary>
        public int entryCount => m_Entries.Count;

        void Awake()
        {
            m_KeyStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 5, 5),
                normal = {textColor = Color.white}
            };

            m_ValueStyle = new GUIStyle
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
            if (!m_Entries.ContainsKey(labeler))
            {
                m_Entries[labeler] = new Dictionary<string, string>();
            }
            m_Entries[labeler][key] = value;
        }

        Vector2 m_ScrollPosition;

        bool m_GUIStylesInitialized = false;

        void SetUpGUIStyles()
        {
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.font = Resources.Load<Font>("Inter-Light");
            GUI.skin.label.padding = new RectOffset(0, 0, 1, 1);
            GUI.skin.label.margin = new RectOffset(0, 0, 1, 1);
            GUI.skin.label.wordWrap = false;
            GUI.skin.label.clipping = TextClipping.Clip;
            GUI.skin.box.padding = new RectOffset(5, 5, 5, 5);
            GUI.skin.toggle.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.horizontalSlider.margin = new RectOffset(0, 0, 0, 0);
            m_GUIStylesInitialized = true;
        }

        int EntriesCount()
        {
            return m_Entries.Count + m_Entries.Sum(entry => entry.Value.Count);
        }

        internal void OnDrawGUI()
        {
            if (m_Entries.Count == 0) return;

            if (!m_GUIStylesInitialized) SetUpGUIStyles();

            GUI.depth = 0; // Draw HUD objects on the top of other UI objects

            var height = Math.Min(k_LineHeight * EntriesCount(), Screen.height * 0.5f - k_YPadding * 2);
            var xPos = Screen.width - k_BoxWidth - k_XPadding;
            var yPos = Screen.height - height - k_YPadding;

            GUILayout.BeginArea(new Rect(xPos, yPos, k_BoxWidth, height), GUI.skin.box);

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

            var firstTime = true;
            foreach (var labeler in m_Entries.Keys)
            {
                if (!firstTime) GUILayout.Space(k_YLineSpacing);
                firstTime = false;
                GUILayout.Label(labeler.GetType().Name);
                foreach (var entry in m_Entries[labeler])
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(5);
                    var k = new StringBuilder(entry.Key.Substring(0, Math.Min(entry.Key.Length, k_MaxKeyLength)));
                    if (k.Length != entry.Key.Length)
                        k.Append("...");
                    GUILayout.Label(k.ToString());
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
            if (m_Entries.ContainsKey(labeler))
            {
                m_Entries[labeler].Remove(key);
            }
        }

        /// <summary>
        /// Removes all of the passed in entries from the HUD
        /// </summary>
        /// <param name="labeler">The labeler that requested the removal</param>
        public void RemoveEntries(CameraLabeler labeler)
        {
            m_Entries.Remove(labeler);
        }
    }
}
