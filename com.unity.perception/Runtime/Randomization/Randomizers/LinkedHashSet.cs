using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Utilities
{
    /// <summary>
    /// This collection has the properties of a HashSet that also preserves insertion order. As such, this data
    /// structure demonstrates the following time complexities:
    /// O(1) lookup, O(1) insertion, O(1) removal, and O(n) traversal
    /// </summary>
    /// <typeparam name="T">The item type to store in this collection</typeparam>
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers")]
    class LinkedHashSet<T> : ICollection<T>
    {
        readonly IDictionary<T, LinkedListNode<T>> m_Dictionary;
        readonly LinkedList<T> m_LinkedList;

        public LinkedHashSet() : this(EqualityComparer<T>.Default) {}

        public LinkedHashSet(IEqualityComparer<T> comparer)
        {
            m_Dictionary = new Dictionary<T, LinkedListNode<T>>(comparer);
            m_LinkedList = new LinkedList<T>();
        }

        public int Count => m_Dictionary.Count;

        public bool IsReadOnly => m_Dictionary.IsReadOnly;

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public bool Add(T item)
        {
            if (m_Dictionary.ContainsKey(item)) return false;
            var node = m_LinkedList.AddLast(item);
            m_Dictionary.Add(item, node);
            return true;
        }

        public void Clear()
        {
            m_LinkedList.Clear();
            m_Dictionary.Clear();
        }

        public bool Remove(T item)
        {
            var found = m_Dictionary.TryGetValue(item, out var node);
            if (!found) return false;
            m_Dictionary.Remove(item);
            m_LinkedList.Remove(node);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_LinkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return m_Dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_LinkedList.CopyTo(array, arrayIndex);
        }
    }
}
