using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Organizes RandomizerTags present in the scene
    /// </summary>
    public class RandomizerTagManager
    {
        /// <summary>
        /// Returns the singleton RandomizerTagManager instance
        /// </summary>
        public static RandomizerTagManager singleton { get; } = new RandomizerTagManager();

        Dictionary<Type, HashSet<Type>> m_TypeTree = new Dictionary<Type, HashSet<Type>>();
        Dictionary<Type, LinkedHashSet<RandomizerTag>> m_TagMap = new Dictionary<Type, LinkedHashSet<RandomizerTag>>();

        /// <summary>
        /// Enumerates over all RandomizerTags of the given type present in the scene
        /// </summary>
        /// <typeparam name="T">The type of RandomizerTag to query for</typeparam>
        /// <param name="returnSubclasses">Should this method retrieve all tags derived from the passed in tag also?</param>
        /// <returns>RandomizerTags of the given type</returns>
        public IEnumerable<T> Query<T>(bool returnSubclasses = false) where T : RandomizerTag
        {
            var queriedTagType = typeof(T);
            if (!m_TagMap.ContainsKey(queriedTagType))
                yield break;

            if (returnSubclasses)
            {
                var typeStack = new Stack<Type>();
                typeStack.Push(queriedTagType);
                while (typeStack.Count > 0)
                {
                    var tagType = typeStack.Pop();
                    foreach (var derivedType in m_TypeTree[tagType])
                        typeStack.Push(derivedType);
                    foreach (var tag in m_TagMap[tagType])
                        yield return (T)tag;
                }
            }
            else
            {
                foreach (var tag in m_TagMap[queriedTagType])
                    yield return (T)tag;
            }
        }

        internal void AddTag<T>(T tag) where T : RandomizerTag
        {
            var tagType = tag.GetType();
            AddTagTypeToTypeHierarchy(tagType);
            m_TagMap[tagType].Add(tag);
        }

        void AddTagTypeToTypeHierarchy(Type tagType)
        {
            if (m_TypeTree.ContainsKey(tagType))
                return;

            m_TagMap.Add(tagType, new LinkedHashSet<RandomizerTag>());
            m_TypeTree.Add(tagType, new HashSet<Type>());

            var baseType = tagType.BaseType;
            while (baseType != null && baseType != typeof(RandomizerTag))
            {
                if (!m_TypeTree.ContainsKey(baseType))
                {
                    m_TagMap.Add(baseType, new LinkedHashSet<RandomizerTag>());
                    m_TypeTree[baseType] = new HashSet<Type> { tagType };
                }
                else
                {
                    m_TypeTree[baseType].Add(tagType);
                    break;
                }

                tagType = baseType;
                baseType = tagType.BaseType;
            }
        }

        internal void RemoveTag<T>(T tag) where T : RandomizerTag
        {
            var tagType = tag.GetType();
            if (m_TagMap.ContainsKey(tagType) && m_TagMap[tagType].Contains(tag))
                m_TagMap[tagType].Remove(tag);
        }
    }
}
