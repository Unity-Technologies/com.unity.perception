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
        Dictionary<Type, List<Type>> m_TypeCache = new Dictionary<Type, List<Type>>();
        Dictionary<Type, HashSet<GameObject>> m_TagMap = new Dictionary<Type, HashSet<GameObject>>();
        Dictionary<GameObject, HashSet<Type>> m_ObjectTags = new Dictionary<GameObject, HashSet<Type>>();

        /// <summary>
        /// Enumerates all GameObjects in the scene that have a RandomizerTag of the given type
        /// </summary>
        /// <typeparam name="T">The type of RandomizerTag to query for</typeparam>
        /// <param name="returnSubclasses">Should this method retrieve all tags derived from the passed in tag also?</param>
        /// <returns>GameObjects with the given RandomizerTag</returns>
        public IEnumerable<GameObject> Query<T>(bool returnSubclasses = false) where T : RandomizerTag
        {
            var type = typeof(T);

            if (!m_TagMap.ContainsKey(type))
                yield break;

            foreach (var taggedObject in m_TagMap[type])
            {
                if (returnSubclasses)
                {
                    yield return taggedObject.gameObject;
                }
                else
                {
                    if (m_ObjectTags.ContainsKey(taggedObject) && m_ObjectTags[taggedObject].Contains(type))
                        yield return taggedObject.gameObject;
                }
            }
        }

        internal void AddTag(Type tagType, GameObject obj)
        {
            // Add tag to the game object to tag map
            if (!m_ObjectTags.ContainsKey(obj))
                m_ObjectTags[obj] = new HashSet<Type>();

            m_ObjectTags[obj].Add(tagType);

            if (!m_TypeCache.ContainsKey(tagType))
            {
                var inheritedTags = new List<Type>();

                while (tagType != null && tagType != typeof(RandomizerTag))
                {
                    inheritedTags.Add(tagType);
                    tagType = tagType.BaseType;
                }

                m_TypeCache[tagType] = inheritedTags;
            }

            var tags = m_TypeCache[tagType];

            foreach (var tag in tags)
            {
                if (!m_TagMap.ContainsKey(tag))
                    m_TagMap[tag] = new HashSet<GameObject>();
                m_TagMap[tag].Add(obj);
            }
        }

        internal void RemoveTag(Type tagType, GameObject obj)
        {
            // Grab all of the tags from the inheritance tree, and remove
            // the passed in object from all of them
            if (m_TypeCache.ContainsKey(tagType))
            {
                var tags = m_TypeCache[tagType];

                foreach (var tag in tags.Where(tag => m_TagMap.ContainsKey(tag)))
                {
                    m_TagMap[tag].Remove(obj);
                }
            }

            // Remove entry from the object to tag map
            if (m_ObjectTags.ContainsKey(obj))
            {
                m_ObjectTags[obj].Remove(tagType);

                if (!m_ObjectTags[obj].Any())
                    m_ObjectTags.Remove(obj);
            }
        }
    }
}
