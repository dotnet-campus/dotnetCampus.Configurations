using System;
using System.Collections.Generic;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 为跨进程的键值对的读写提供安全的读写上下文。
    /// </summary>
    /// <typeparam name="TKey">键。</typeparam>
    /// <typeparam name="TValue">值。</typeparam>
    public interface ICriticalReadWriteContext<TKey, TValue>
    {
        /// <summary>
        /// 在进程安全的上下文中，当读到键值集合后请调用此方法将值传入以得到合并后的键值集合。
        /// </summary>
        /// <param name="keyValues">在进程安全的上下文中读到的键值集合。</param>
        /// <param name="externalUpdateTime">外部值的最近更新时间。</param>
        public TimedKeyValues<TKey, TValue> MergeExternalKeyValues(IReadOnlyDictionary<TKey, TValue> keyValues, DateTimeOffset? externalUpdateTime = null);
    }
}
