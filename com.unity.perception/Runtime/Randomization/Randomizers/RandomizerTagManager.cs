using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Organizes RandomizerTags attached to GameObjects in the scene
    /// </summary>
    public class RandomizerTagManager
    {
        Dictionary<Type, HashSet<GameObject>> m_TagMap = new Dictionary<Type, HashSet<GameObject>>();

        /// <summary>
        /// Returns a list of all GameObjects in the scene that have a RandomizerTag of the given type
        /// </summary>
        /// <typeparam name="T">The type of RandomizerTag to query for</typeparam>
        /// <returns>A list of GameObjects with the given RandomizerTag</returns>
        public GameObject[] Query<T>() where T : RandomizerTag
        {
            var type = typeof(T);
            return m_TagMap.ContainsKey(type) ? m_TagMap[type].ToArray() : new GameObject[0];
        }

        internal void AddTag(Type tagType, GameObject obj)
        {
            if (m_TagMap.ContainsKey(tagType))
            {
                m_TagMap[tagType].Add(obj);
            }
            else
            {
                var newSet = new HashSet<GameObject> { obj };
                m_TagMap.Add(tagType, newSet);
            }
        }

        internal void RemoveTag(Type tagType, GameObject obj)
        {
            if (m_TagMap.ContainsKey(tagType))
                m_TagMap[tagType].Remove(obj);
        }
    }
}
