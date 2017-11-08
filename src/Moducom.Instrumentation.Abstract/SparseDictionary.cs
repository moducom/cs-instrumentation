﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moducom.Instrumentation.Abstract
{
    public struct SparseDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>
    {
        LazyLoader<Dictionary<TKey, TValue>> value;

        /*
        public SparseDictionary(IDictionary<TKey, TValue> copyFrom)
        {
            value.Value = new Dictionary<TKey, TValue>(copyFrom);
        } */

        public TValue this[TKey key]
        {
            get
            {
                if (value.IsAllocated) return value.Value[key];

                throw new KeyNotFoundException();
            }
            set => this.value.Value[key] = value;
        }

        public ICollection<TKey> Keys => throw new NotImplementedException();

        public ICollection<TValue> Values
        {
            get
            {
                if (value.IsAllocated) return value.Value.Values;

                // Can't use .Empty because that's just an IEnumeration
                return new TValue[0];
            }
        }

        public int Count => value.IsAllocated ? value.Value.Count : 0;

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(TKey key, TValue value)
        {
            this.value.Value.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)value.Value).Add(item);
        }

        public void Clear()
        {
            if (value.IsAllocated) value.Value.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key)
        {
            if (value.IsAllocated) return value.Value.ContainsKey(key);

            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (value.IsAllocated) return value.Value.GetEnumerator();

            return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
