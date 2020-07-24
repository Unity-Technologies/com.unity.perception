using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    ///
    /// </summary>
    public class KeyValuePanel : MonoBehaviour
    {
        public Text key = null;
        public Text value = null;

        /// <summary>
        /// Sets the key of this key value pair
        /// </summary>
        public void SetKey(string k)
        {
            if (k == null || k == string.Empty) return;
            key.text = k;
        }

        /// <summary>
        /// Sets the value of this key value pair
        /// </summary>
        public void SetValue(string v)
        {
            value.text = v;
        }
    }
}
