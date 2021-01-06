using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Organizes RandomizerTags attached to GameObjects in the scene
    /// </summary>
    public class RandomizerTagManager
    {
        Dictionary<Type, HashSet<Type>> m_TypeTree = new Dictionary<Type, HashSet<Type>>();
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
                var typeStack = new Stack<Type>();
                typeStack.Push(queriedTagType);
                while (typeStack.Count > 0)
                {
                    var tagType = typeStack.Pop();
                    foreach (var derivedType in m_TypeTree[tagType])
                        typeStack.Push(derivedType);
                    foreach (var obj in m_TagMap[tagType])
                        yield return obj;
                }
            }
            else
            {
                foreach (var obj in m_TagMap[queriedTagType])
                    yield return obj;
            }
        }

        internal void AddTag(Type tagType, GameObject obj)
        {
            AddTagTypeToTypeHierarchy(tagType);
            m_TagMap[tagType].Add(obj);
        }

        void AddTagTypeToTypeHierarchy(Type tagType)
        {
            if (m_TypeTree.ContainsKey(tagType))
                return;

            if (tagType == null || !tagType.IsSubclassOf(typeof(RandomizerTag)))
                throw new ArgumentException("Tag type is not a subclass of RandomizerTag");

            m_TagMap.Add(tagType, new HashSet<GameObject>());
            m_TypeTree.Add(tagType, new HashSet<Type>());

            var baseType = tagType.BaseType;
            while (baseType!= null && baseType != typeof(RandomizerTag))
            {
                if (!m_TypeTree.ContainsKey(baseType))
                {
                    m_TagMap.Add(baseType, new HashSet<GameObject>());
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

        internal void RemoveTag(Type tagType, GameObject obj)
        {
            if (m_TagMap.ContainsKey(tagType) && m_TagMap[tagType].Contains(obj))
                m_TagMap[tagType].Remove(obj);
        }
    }
}
