#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 跨进程并发的字典。
    /// </summary>
    partial class ProcessConcurrentDictionary<TKey, TValue>
    {
        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => _keyValues.TryGetValue(item.Key, out var value) && Equals(value, item);

        /// <inheritdoc />
        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            if (_keyValues.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <inheritdoc />
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => _keyValues.TryAdd(key, CreateInternalValue(value));

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => _keyValues.TryAdd(item.Key, CreateInternalValue(item.Value));

        /// <inheritdoc />
        bool IDictionary<TKey, TValue>.Remove(TKey key) => _keyValues.TryRemove(key, out _);

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => _keyValues.TryRemove(item.Key, out _);

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => _keyValues.Clear();

        /// <inheritdoc />
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => _keyValues.Keys;

        /// <inheritdoc />
        ICollection<TValue> IDictionary<TKey, TValue>.Values => _keyValues.Values.Select(x => x.Value).ToList();

        /// <inheritdoc />
        int ICollection<KeyValuePair<TKey, TValue>>.Count => _keyValues.Count;

        /// <inheritdoc />
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <inheritdoc />
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "序号必须大于等于 0。");
            }

            _keyValues.ToArray().CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            foreach (var pair in _keyValues)
            {
                yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
    }
}
