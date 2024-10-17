using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Verse;
using Verse.Noise;

namespace NoPrepatcherFieldProvider
{
    [Serializable]
    public class RefDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        protected class RefEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            protected List<KeyValuePair<TKey, TValue>> _keyValuePairs;
            public int position = -1;

            protected bool disposed = false;

            public RefEnumerator(IEnumerable<KeyValuePair<TKey, Wrapper>> backingEnumerable)
            {
                List<KeyValuePair<TKey, TValue>> keyValuePairs = new List<KeyValuePair<TKey, TValue>>(backingEnumerable.Count());
                foreach (var item in backingEnumerable)
                    keyValuePairs.Add(new KeyValuePair<TKey, TValue>(item.Key, item.Value.value));
                _keyValuePairs = keyValuePairs;
            }
            
            public KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (disposed)
                        throw new ObjectDisposedException(GetType().FullName);
                    
                    try
                    {
                        return _keyValuePairs[position];
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw new InvalidOperationException("", ex);
                    }
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                
                position++;
                return position < _keyValuePairs.Count && position >= 0;
            }

            public bool MovePrevious()
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                position--;
                return position >= 0 && position < _keyValuePairs.Count;
            }

            public void Reset()
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().FullName);

                position = -1;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    _keyValuePairs.Clear();
                    _keyValuePairs = null;
                    position = 0;
                }

                disposed = true;
            }
        }

        [Serializable]
        protected class Wrapper
        {
            public TValue value;

            public Wrapper()
            {
            }

            public Wrapper(TValue value)
            {
                this.value = value;
            }

            public static bool operator ==(Wrapper wrapper1, Wrapper wrapper2) => wrapper1.value.Equals(wrapper2.value);
            public static bool operator !=(Wrapper wrapper1, Wrapper wrapper2) => !(wrapper1.value.Equals(wrapper2.value));

            public static implicit operator TValue(Wrapper wrapper) { return wrapper.value; }
        }

        protected IDictionary<TKey, Wrapper> backingDictionary;

        private List<TValue> valuesCached;
        private bool valuesCacheValid = false;

        public ICollection<TKey> Keys => backingDictionary.Keys;

        public ICollection<TValue> Values
        {
            get
            {
                if (!valuesCacheValid)
                {
                    var valueWrappers = backingDictionary.Values;

                    valuesCached = new List<TValue>(backingDictionary.Count);
                    foreach (Wrapper valueWrapper in valueWrappers)
                        valuesCached.Add(valueWrapper);
                    
                    valuesCacheValid = true;
                }
                return valuesCached;
            }
        }

        public int Count => backingDictionary.Count;

        public bool IsReadOnly => false;

        TValue IDictionary<TKey, TValue>.this[TKey key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Dictionary<TKey, Mutex> mutexesForKeys = new Dictionary<TKey, Mutex>();
        Mutex mutexForMutexDictionary = new Mutex();
        public ref TValue this[TKey key]
        {
            get
            {
                mutexForMutexDictionary.WaitOne();
                if (!mutexesForKeys.ContainsKey(key))
                    mutexesForKeys[key] = new Mutex();
                mutexForMutexDictionary.ReleaseMutex();

                mutexesForKeys[key].WaitOne();
                if (backingDictionary.TryGetValue(key, out Wrapper wrapper))
                {
                    mutexesForKeys[key].ReleaseMutex();
                    return ref wrapper.value;
                }
                else
                {
                    TValue emptyValue = default;
                    
                    Wrapper emptyWrapper = new Wrapper(emptyValue);
                    backingDictionary.Add(key, emptyWrapper);

                    mutexesForKeys[key].ReleaseMutex();
                    return ref emptyWrapper.value;
                }
            }
        }

        public bool ContainsKey(TKey key) => backingDictionary.ContainsKey(key);

        public void Add(TKey key, TValue value) => backingDictionary.Add(key, new Wrapper(value));

        public bool Remove(TKey key) => backingDictionary.Remove(key);

        private static int DefaultConcurrencyLevel => Environment.ProcessorCount;
        public RefDictionary()
        {
            backingDictionary = new ConcurrentDictionary<TKey, Wrapper>();
        }

        public RefDictionary(int capacity)
        {
            backingDictionary = new ConcurrentDictionary<TKey, Wrapper>(DefaultConcurrencyLevel, capacity);
        }

        public RefDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            IEnumerable<KeyValuePair<TKey, Wrapper>> keyWrapperPairs = collection.Select(kvp => new KeyValuePair<TKey, Wrapper>(kvp.Key, new Wrapper(kvp.Value)));
            backingDictionary = new ConcurrentDictionary<TKey, Wrapper>(keyWrapperPairs);
        }

        public RefDictionary(IDictionary<TKey, TValue> dictionary)
        {
            Dictionary<TKey, Wrapper> wrapperDictionary = new Dictionary<TKey, Wrapper>(dictionary.Select(kvp => new KeyValuePair<TKey, Wrapper>(kvp.Key, new Wrapper(kvp.Value))));
            backingDictionary = new ConcurrentDictionary<TKey, Wrapper>(wrapperDictionary);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            bool gotValue = backingDictionary.TryGetValue(key, out Wrapper wrapper);

            wrapper = wrapper ?? new Wrapper();
            value = wrapper.value;

            return gotValue;
        }

        public void Add(KeyValuePair<TKey, TValue> item) => backingDictionary.Add(item.Key, new Wrapper(item.Value));

        public void Clear() => backingDictionary.Clear();

        public bool Contains(KeyValuePair<TKey, TValue> item) => backingDictionary.ContainsKey(item.Key) && backingDictionary[item.Key] == new Wrapper(item.Value);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
                return backingDictionary.Remove(item.Key);
            else
                return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => new RefEnumerator(backingDictionary);

        IEnumerator IEnumerable.GetEnumerator() => new RefEnumerator(backingDictionary);
    }
}
