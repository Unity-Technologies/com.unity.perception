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
        Dictionary<Type, HashSet<Type>> m_TypeCache = new Dictionary<Type, HashSet<Type>>();
        Dictionary<Type, HashSet<GameObject>> m_TagMap = new Dictionary<Type, HashSet<GameObject>>();

        /// <summary>
        /// Enumerates all GameObjects in the scene that have a RandomizerTag of the given type
        /// </summary>
        /// <typeparam name="T">The type of RandomizerTag to query for</typeparam>
        /// <param name="returnSubclasses">Should this method retrieve all tags derived from the passed in tag also?</param>
        /// <returns>GameObjects with the given RandomizerTag</returns>
        public IEnumerable<GameObject> Query<T>(bool returnSubclasses = false) where T : RandomizerTag
        {
            var queriedTagType = typeof(T);
            if (!m_TagMap.ContainsKey(queriedTagType))
                yield break;

            if (returnSubclasses)
            {
                foreach (var tagType in m_TypeCache[queriedTagType])
                {
                    foreach (var obj in m_TagMap[tagType])
                        yield return obj;
                }
            }
            else
            {
                foreach (var taggedObject in m_TagMap[queriedTagType])
                    yield return taggedObject.gameObject;
            }
        }

        internal void AddTag(Type tagType, GameObject obj)
        {
            if (!m_TypeCache.ContainsKey(tagType))
                AddTagTypeAndBaseTagTypesToCache(tagType);
            m_TagMap[tagType].Add(obj);
        }

        void AddTagTypeAndBaseTagTypesToCache(Type tagType)
        {
            var recursiveTagType = tagType;
            var inheritedTags = new HashSet<Type>();
            while (recursiveTagType != null && recursiveTagType != typeof(RandomizerTag))
            {
                inheritedTags.Add(recursiveTagType);
                if (!m_TagMap.ContainsKey(recursiveTagType))
                    m_TagMap[recursiveTagType] = new HashSet<GameObject>();
                recursiveTagType = recursiveTagType.BaseType;
            }
            m_TypeCache[tagType] = inheritedTags;
        }

        internal void RemoveTag(Type tagType, GameObject obj)
        {
            if (m_TagMap.ContainsKey(tagType) && m_TagMap[tagType].Contains(obj))
                m_TagMap[tagType].Remove(obj);
        }
    }
}
