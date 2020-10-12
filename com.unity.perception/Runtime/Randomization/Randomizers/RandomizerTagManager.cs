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
        /// Enumerates all GameObjects in the scene that have a RandomizerTag of the given type
        /// </summary>
        /// <typeparam name="T">The type of RandomizerTag to query for</typeparam>
        /// <returns>GameObjects with the given RandomizerTag</returns>
        public IEnumerable<GameObject> Query<T>() where T : RandomizerTag
        {
            var type = typeof(T);
            if (!m_TagMap.ContainsKey(type))
                yield break;
            foreach (var taggedObject in m_TagMap[type])
                yield return taggedObject;
        }

        internal void AddTag(Type tagType, GameObject obj)
        {
            if (!m_TagMap.ContainsKey(tagType))
                m_TagMap[tagType] = new HashSet<GameObject>();
            m_TagMap[tagType].Add(obj);
        }

        internal void RemoveTag(Type tagType, GameObject obj)
        {
            if (m_TagMap.ContainsKey(tagType))
                m_TagMap[tagType].Remove(obj);
        }
    }
}
