#nullable disable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using dotnetCampus.Configurations.Utils;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 跨进程并发的字典。
    /// </summary>
    internal partial class ProcessConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, ProcessSafeValueEntry<TValue>> _keyValues = new ConcurrentDictionary<TKey, ProcessSafeValueEntry<TValue>>();

        public ICollection<TKey> Keys => _keyValues.Keys;

        public TValue this[TKey key]
        {
            get => _keyValues.TryGetValue(key, out var entry) ? entry.Value : default;
            set => _ = _keyValues.AddOrUpdate(
                key,
                _ => CreateInternalValue(value),
                (_, existed) => CreateInternalValue(value, existed));
        }

        /// <summary>
        /// 要求从外部存储中更新所有值。因为存在进程锁，所以此方法可能会耗时。
        /// </summary>
        /// <param name="file">指定一个文件路径。不同进程间指定相同文件时，从外部源更新键值则是进程安全的。</param>
        /// <param name="duringCriticalReadWriteContext">
        /// 进程安全的代码块，请在此代码块中从外部源读取键值集合，
        /// 调用 <see cref="ICriticalReadWriteContext{TKey, TValue}.MergeExternalKeyValues(IReadOnlyDictionary{TKey, TValue}, DateTimeOffset?)"/>，
        /// 然后将返回的已合并键值集合写回外部源。
        /// </param>
        public void UpdateValuesFromExternal(FileInfo file,
            Action<ICriticalReadWriteContext<TKey, TValue>> duringCriticalReadWriteContext)
        {
            var path = Path.GetFullPath(file.FullName);
            var name = OSUtils.IsPathCaseSensitive() ? path : path.ToUpperInvariant();
            var comparison = OSUtils.IsPathCaseSensitive() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
#if NETFRAMEWORK || NETSTANDARD
            name = name.Replace("/", "_").Replace("\\", "_");
#else
            name = name.Replace("/", "_", comparison).Replace("\\", "_", comparison);
#endif

            UpdateValuesFromExternal(name, duringCriticalReadWriteContext);
        }

        /// <summary>
        /// 要求从外部存储中更新所有值。因为存在进程锁，所以此方法可能会耗时。
        /// </summary>
        /// <param name="name">一个用于区分同一个外部源的名称。不同进程间指定相同名称时，从外部源更新键值则是进程安全的。</param>
        /// <param name="duringCriticalReadWriteContext">
        /// 进程安全的代码块，请在此代码块中从外部源读取键值集合，
        /// 调用 <see cref="ICriticalReadWriteContext{TKey, TValue}.MergeExternalKeyValues(IReadOnlyDictionary{TKey, TValue}, DateTimeOffset?)"/>，
        /// 然后将返回的已合并键值集合写回外部源。
        /// </param>
        public void UpdateValuesFromExternal(string name,
            Action<ICriticalReadWriteContext<TKey, TValue>> duringCriticalReadWriteContext)
        {
            using var mutex = new Mutex(false, name);
            try
            {
                mutex.WaitOne();
            }
            catch (AbandonedMutexException)
            {
                // 发现被遗弃的锁（其他已退出进程）。已获取到，并可用。
            }

            try
            {
                var context = new CriticalReadWriteContext<TKey, TValue>(UpdateValuesFromExternalCore);
                duringCriticalReadWriteContext(context);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// 如果存在键为 <paramref name="key"/> 的值就返回 true，否则返回 false。
        /// </summary>
        /// <param name="key">要查找的键。</param>
        /// <returns>如果存在就返回 true，否则返回 false。</returns>
        public bool ContainsKey(TKey key) => _keyValues.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value)
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

        public bool TryRemove(TKey key, out TValue value)
        {
            if (_keyValues.TryRemove(key, out var entry))
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

        private IReadOnlyDictionary<TKey, TValue> UpdateValuesFromExternalCore(
            IReadOnlyDictionary<TKey, TValue> externalKeyValues,
            DateTimeOffset externalUpdateTime)
        {
            // 以下代码块虽然没有加锁，但可以确保所有外部键值消耗完成后的瞬时状态是那个时刻的最终状态。
            var exceptedKeys = _keyValues.Keys.Except(externalKeyValues.Keys);
            foreach (var external in externalKeyValues)
            {
                _keyValues.AddOrUpdate(external.Key,
                    _ => CreateExternalValue(external.Value, externalUpdateTime),
                    (_, existed) => CreateExternalValue(external.Value, existed, externalUpdateTime));
            }
            foreach (var key in exceptedKeys)
            {
                _keyValues.AddOrUpdate(key,
                    _ => CreateDeletedValue(externalUpdateTime),
                    (_, existed) => CreateMergedInternalValue(existed));
            }
            return _keyValues.Where(x => x.Value.State != ProcessSafeValueState.Deleted).ToDictionary(x => x.Key, x => x.Value.Value);
        }

        /// <summary>
        /// 创建一个由内部值新建的 <see cref="ProcessSafeValueEntry{TValue}"/>。
        /// </summary>
        /// <param name="value">新建的值。</param>
        /// <returns>进程安全的值。</returns>
        private static ProcessSafeValueEntry<TValue> CreateInternalValue(TValue value) => new ProcessSafeValueEntry<TValue>(
            value,
            default,
            DateTimeOffset.UtcNow,
            ProcessSafeValueState.Changed);

        /// <summary>
        /// 创建一个由内部值新建的 <see cref="ProcessSafeValueEntry{TValue}"/>。
        /// 根据外部引入的 <see cref="ProcessSafeValueEntry{TValue}"/> 创建一个合并了内存中值的新 <see cref="ProcessSafeValueEntry{TValue}"/>。
        /// </summary>
        /// <param name="value">新建的值。</param>
        /// <param name="existedEntry">外部引入的值。</param>
        /// <returns>进程安全的值。</returns>
        private static ProcessSafeValueEntry<TValue> CreateInternalValue(TValue value, ProcessSafeValueEntry<TValue> existedEntry)
            => Equals(value, existedEntry.Value)
                ? existedEntry
                : new ProcessSafeValueEntry<TValue>(
                    value,
                    existedEntry.ExternalValue,
                    DateTimeOffset.UtcNow,
                    ProcessSafeValueState.Changed);

        /// <summary>
        /// 将当前内存中存储的值复制一遍，然后标记此值已合并。
        /// </summary>
        /// <param name="existedEntry">当前内存中已经存在的值。</param>
        /// <returns>进程安全的值。</returns>
        private static ProcessSafeValueEntry<TValue> CreateMergedInternalValue(ProcessSafeValueEntry<TValue> existedEntry)
            => new ProcessSafeValueEntry<TValue>(
                existedEntry.Value,
                existedEntry.Value,
                existedEntry.LastUpdateTime,
                ProcessSafeValueState.NotChanged);

        /// <summary>
        /// 创建一个由外部值引入的 <see cref="ProcessSafeValueEntry{TValue}"/>。
        /// </summary>
        /// <param name="value">外部引入的值。</param>
        /// <param name="externalUpdateTime">外部值的最近更新时间。</param>
        /// <returns>进程安全的值。</returns>
        private static ProcessSafeValueEntry<TValue> CreateExternalValue(TValue value, DateTimeOffset externalUpdateTime)
            => new ProcessSafeValueEntry<TValue>(
                value,
                default,
                externalUpdateTime,
                ProcessSafeValueState.NotChanged);

        /// <summary>
        /// 根据当前内存中已存在的 <see cref="ProcessSafeValueEntry{TValue}"/> 创建一个合并了外部值的新 <see cref="ProcessSafeValueEntry{TValue}"/>。
        /// </summary>
        /// <param name="value">外部引入的值。</param>
        /// <param name="existedEntry">内存中已存在的值。</param>
        /// <param name="externalUpdateTime">外部值的最近更新时间。</param>
        /// <returns>进程安全的值。</returns>
        private static ProcessSafeValueEntry<TValue> CreateExternalValue(TValue value, ProcessSafeValueEntry<TValue> existedEntry, DateTimeOffset externalUpdateTime)
        {
            if (Equals(value, existedEntry.Value))
            {
                // 相同值，任意返回。
                return existedEntry;
            }
            else if (existedEntry.LastUpdateTime > externalUpdateTime)
            {
                // 值在内存中有更新。
                return new ProcessSafeValueEntry<TValue>(
                    value,
                    existedEntry.ExternalValue,
                    existedEntry.LastUpdateTime,
                    ProcessSafeValueState.Changed);
            }
            else
            {
                // 值在外部有更新。
                return new ProcessSafeValueEntry<TValue>(
                    value,
                    existedEntry.ExternalValue,
                    externalUpdateTime,
                    ProcessSafeValueState.NotChanged);
            }
        }

        /// <summary>
        /// 创建一个已删除的 <see cref="ProcessSafeValueEntry{TValue}"/>。
        /// </summary>
        /// <param name="deletedTime">已知的最近删除时间。</param>
        /// <returns>进程安全的值。</returns>
        private static ProcessSafeValueEntry<TValue> CreateDeletedValue(DateTimeOffset deletedTime)
            => new ProcessSafeValueEntry<TValue>(
                default,
                default,
                deletedTime,
                ProcessSafeValueState.Deleted);
    }
}
