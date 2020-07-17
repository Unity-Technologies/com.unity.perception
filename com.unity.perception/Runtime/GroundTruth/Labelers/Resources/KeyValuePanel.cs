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
        Text key = null;
        Text value = null;

        void Awake()
        {
            key = this.transform.Find("Key").GetComponent<Text>();
            value = this.transform.Find("Value").GetComponent<Text>();
        }

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
