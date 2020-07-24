using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Heads up display panel used to publish a key value pair on the screen.
    /// </summary>
    public class HUDPanel : MonoBehaviour
    {
        Dictionary<string, KeyValuePanel> entries = new Dictionary<string, KeyValuePanel>();
        Image img = null;

        void Awake() 
        {
            img = GetComponentInChildren<Image>();
        }

        void Update()
        {
            // TODO - is this slow? If so, figure out a faster way to do this...
            img.enabled = entries.Any();
        }

        /// <summary>
        /// Updates (or creates) an entry with the passed in key value pair
        /// </summary>
        public void UpdateEntry(string key, string value)
        {
            if (!entries.ContainsKey(key)) 
            {
                entries[key] = GameObject.Instantiate(Resources.Load<GameObject>("KeyValuePanel")).GetComponent<KeyValuePanel>();
                entries[key].SetKey(key);
                entries[key].transform.SetParent(this.transform, false);
            }
            entries[key].SetValue(value);
        }

        /// <summary>
        /// Removes the key value pair from the HUD
        /// </summary>
        public void RemoveEntry(string key)
        {
            if (entries.ContainsKey(key))
            {
                var pair = entries[key];
                entries.Remove(key);

                pair.transform.SetParent(null);
                Destroy(pair.gameObject);
            }
        }
    }
}
