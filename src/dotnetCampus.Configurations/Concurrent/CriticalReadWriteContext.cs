using System;
using System.Collections.Generic;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 为跨进程的键值对的读写提供安全的读写上下文。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    internal class CriticalReadWriteContext<TKey, TValue> : ICriticalReadWriteContext<TKey, TValue>
    {
        private readonly Func<IReadOnlyDictionary<TKey, TValue>, DateTimeOffset, TimedKeyValues<TKey, TValue>> _keyValueMergingFunc;
        
        /// <summary>
        /// 创建 <see cref="CriticalReadWriteContext{TKey, TValue}"/> 的新实例。
        /// </summary>
        /// <param name="keyValueMergingFunc">请在此委托中合并键值集合。</param>
        public CriticalReadWriteContext(Func<IReadOnlyDictionary<TKey, TValue>, DateTimeOffset,
            TimedKeyValues<TKey, TValue>> keyValueMergingFunc)
        {
            _keyValueMergingFunc = keyValueMergingFunc ?? throw new ArgumentNullException(nameof(keyValueMergingFunc));
        }

        /// <inheritdoc />
        public TimedKeyValues<TKey, TValue> MergeExternalKeyValues(IReadOnlyDictionary<TKey, TValue> keyValues, DateTimeOffset? externalUpdateTime = null)
            => _keyValueMergingFunc(keyValues, externalUpdateTime ?? DateTimeOffset.UtcNow);
    }
}
