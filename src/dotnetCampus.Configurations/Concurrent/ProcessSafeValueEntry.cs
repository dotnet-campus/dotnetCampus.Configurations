using System;
using System.Diagnostics;

namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 表示一个跨进程安全的值存储空间。
    /// </summary>
    /// <typeparam name="TValue">值的类型。</typeparam>
    [DebuggerDisplay("{Value,nq} [{State}] // {LastUpdateTime}")]
    internal readonly struct ProcessSafeValueEntry<TValue>
    {
        internal ProcessSafeValueEntry(TValue value, TValue externalValue, DateTimeOffset timestamp, ProcessSafeValueState state)
        {
            Value = value;
            ExternalValue = externalValue;
            LastUpdateTime = timestamp;
            State = state;
        }

        /// <summary>
        /// 获取此值的最新修改时间。如果值是外部修改的，则为外部文件的最近写时间；如果值是内部修改的，则为最近赋值的时间。
        /// </summary>
        public DateTimeOffset LastUpdateTime { get; }

        /// <summary>
        /// 获取此值在外部存储中的值。
        /// </summary>
        public TValue ExternalValue { get; }

        /// <summary>
        /// 获取此值当前的最新值。如果尚未修改，它一定与外部值 <see cref="ExternalValue"/> 相等。
        /// </summary>
        public TValue Value { get; }

        /// <summary>
        /// 获取此值在内存中相比于外部存储来说的修改状态。
        /// </summary>
        public ProcessSafeValueState State { get; }
    }
}
