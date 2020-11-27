using System;
using System.Collections.Generic;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 包含时间戳的键值集合。
    /// </summary>
    public readonly struct TimedKeyValues<TKey, TValue> : IEquatable<TimedKeyValues<TKey, TValue>>
    {
        /// <summary>
        /// 创建 <see cref="TimedKeyValues{TKey, TValue}"/> 的新实例。
        /// </summary>
        /// <param name="keyValues">键值集合。</param>
        /// <param name="time">键值集合的更新时间。</param>
        public TimedKeyValues(IReadOnlyDictionary<TKey, TValue> keyValues, DateTimeOffset time)
        {
            KeyValues = keyValues ?? throw new ArgumentNullException(nameof(keyValues));
            Time = time;
        }

        /// <summary>
        /// 键值集合。
        /// </summary>
        public IReadOnlyDictionary<TKey, TValue> KeyValues { get; }

        /// <summary>
        /// 键值集合的更新时间。
        /// </summary>
        public DateTimeOffset Time { get; }

        public override bool Equals(object? obj)
        {
            return obj is TimedKeyValues<TKey, TValue> values && Equals(values);
        }

        public bool Equals(TimedKeyValues<TKey, TValue> other)
        {
            return EqualityComparer<IReadOnlyDictionary<TKey, TValue>>.Default.Equals(KeyValues, other.KeyValues) &&
                   Time.Equals(other.Time);
        }

        public override int GetHashCode()
        {
            var hashCode = -746944874;
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyDictionary<TKey, TValue>>.Default.GetHashCode(KeyValues);
            hashCode = hashCode * -1521134295 + Time.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(TimedKeyValues<TKey, TValue> left, TimedKeyValues<TKey, TValue> right) => left.Equals(right);
        public static bool operator !=(TimedKeyValues<TKey, TValue> left, TimedKeyValues<TKey, TValue> right) => !(left == right);
    }
}
