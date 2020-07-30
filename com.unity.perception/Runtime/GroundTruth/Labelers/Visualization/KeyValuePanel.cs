using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Key value pair panel UI object
    /// </summary>
    public class KeyValuePanel : MonoBehaviour
    {
        public Text key = null;
        public Text value = null;

        /// <summary>
        /// Sets the key of this key value pair
        /// <param name="k">The key of the key/value pair</param>
        /// </summary>
        public void SetKey(string k)
        {
            if (k == null || k == string.Empty) return;
            key.text = k;
        }

        /// <summary>
        /// Sets the value of this key value pair
        /// <param name="v">The value of the key/value pair</param>
        /// </summary>
        public void SetValue(string v)
        {
            value.text = v;
        }
    }
}
