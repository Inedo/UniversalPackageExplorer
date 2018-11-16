using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace UniversalPackageExplorer.Collections
{
    public sealed class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, INotifyCollectionChanged
    {
        private readonly ObservableCollection<KeyValuePair<TKey, TValue>> inner = new ObservableCollection<KeyValuePair<TKey, TValue>>();

        public ObservableDictionary()
        {
            this.Keys = new ComputedCollection<ObservableDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>, TKey>(this, kv => kv.Key);
            this.Values = new ComputedCollection<ObservableDictionary<TKey, TValue>, KeyValuePair<TKey, TValue>, TValue>(this, kv => kv.Value);
        }

        private int? FindIndex(TKey key)
        {
            for (int i = 0; i < inner.Count; i++)
            {
                if (object.Equals(key, inner[i].Key))
                {
                    return i;
                }
            }
            return null;
        }

        public KeyValuePair<TKey, TValue> this[int index]
        {
            get => inner[index];
            set => inner[index] = value;
        }
        public TValue this[TKey key]
        {
            get
            {
                var index = this.FindIndex(key) ?? throw new KeyNotFoundException();
                return inner[index].Value;
            }
            set
            {
                var index = this.FindIndex(key);
                if (index.HasValue)
                {
                    inner[index.Value] = new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    inner.Add(new KeyValuePair<TKey, TValue>(key, value));
                }
            }
        }

        public int Count => inner.Count;

        public bool IsReadOnly => ((IList<KeyValuePair<TKey, TValue>>)inner).IsReadOnly;

        public ICollection<TKey> Keys { get; }
        public ICollection<TValue> Values { get; }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => inner.CollectionChanged += value;
            remove => inner.CollectionChanged -= value;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(TKey key, TValue value)
        {
            if (FindIndex(key).HasValue)
            {
                throw new ArgumentException("Key already exists.", nameof(key));
            }
            inner.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return inner.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return FindIndex(key).HasValue;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            inner.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            return inner.IndexOf(item);
        }

        public void Insert(int index, KeyValuePair<TKey, TValue> item)
        {
            inner.Insert(index, item);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return inner.Remove(item);
        }

        public bool Remove(TKey key)
        {
            var index = FindIndex(key);
            if (index.HasValue)
            {
                RemoveAt(index.Value);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            inner.RemoveAt(index);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var index = FindIndex(key);
            if (index.HasValue)
            {
                value = inner[index.Value].Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.GetEnumerator();
        }
    }
}