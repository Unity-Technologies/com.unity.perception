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
        Dictionary<string, (bool, KeyValuePanel)> entries = new Dictionary<string, (bool, KeyValuePanel)>();
        Stack<KeyValuePanel> orphans = new Stack<KeyValuePanel>();

        public GameObject contentPanel = null;
        public ScrollRect scrollRect = null;
        public Image img = null;

        void Update()
        {
            if (entries.Any() != scrollRect.enabled)
            {
                scrollRect.enabled = !scrollRect.enabled;
                img.enabled = scrollRect.enabled;
                foreach (var i in GetComponentsInChildren<Image>())
                {
                    i.enabled = scrollRect.enabled;
                }
            }

            // Go through everyone that has not been updated and remove them and
            // if they have been updated mark them dirty for next round
            var keys = new List<string>(entries.Keys);
            foreach (var key in keys)
            {
                var entry = entries[key];

                if (!entry.Item1)
                {
                    entry.Item2.gameObject.SetActive(false);
                    orphans.Push(entry.Item2);
                }
                else
                {
                    entry.Item1 = false;
                    entries[key] = entry;
                }
            }
        }

        /// <summary>
        /// Updates (or creates) an entry with the passed in key value pair
        /// </summary>
        public void UpdateEntry(string key, string value)
        {
            (bool, KeyValuePanel) val;

            if (!entries.ContainsKey(key))
            {
                if (orphans.Any())
                {
                    val = (true, orphans.Pop());
                    val.Item2.gameObject.SetActive(true);
                }
                else
                {
                    val = (true, GameObject.Instantiate(Resources.Load<GameObject>("KeyValuePanel")).GetComponent<KeyValuePanel>());
                    val.Item2.transform.SetParent(contentPanel.transform, true);
                }

                val.Item2.SetKey(key);
            }
            else
            {
                val = entries[key];
                val.Item1 = true;
            }

            val.Item2.SetValue(value);
            entries[key] = val;
        }

        /// <summary>
        /// Removes the key value pair from the HUD
        /// </summary>
        public void RemoveEntry(string key)
        {
            if (entries.ContainsKey(key))
            {
                var entry = entries[key];
                entry.Item2.gameObject.SetActive(false);
                entries.Remove(key);
                orphans.Push(entry.Item2);
            }
        }

        /// <summary>
        /// Removes all of the passed in entries from the HUD
        /// </summary>
        public void RemoveEntries(List<string> keys)
        {
            foreach (var k in keys) RemoveEntry(k);
        }

        /// <summary>
        /// Removes all of the passed in entries from the HUD
        /// </summary>
        public void RemoveEntries(string[] keys)
        {
            foreach (var k in keys) RemoveEntry(k);
        }
    }
}
