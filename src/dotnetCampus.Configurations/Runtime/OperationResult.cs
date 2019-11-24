using System;

#pragma warning disable CA2225 // Operator overloads have named alternates
#pragma warning disable CA1815 // Override equals and operator equals on value types
// ReSharper disable once CheckNamespace

namespace dotnetCampus.Threading
{
    /// <summary>
    /// 为一个操作包装结果信息，包括成功与否、异常和取消信息。
    /// </summary>
    public readonly struct OperationResult
    {
        /// <summary>
        /// 使用指定的异常创建 <see cref="OperationResult"/> 的新实例。
        /// 这个操作结果是失败的。
        /// </summary>
        /// <param name="exception">操作过程中收集到的异常。</param>
        public OperationResult(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            IsCanceled = false;
        }

        /// <summary>
        /// 创建一个成功的或者取消的 <see cref="OperationResult"/>。
        /// </summary>
        /// <param name="isSuccessOrCanceled">
        /// 如果为 true，则创建一个成功的操作结果；如果为 false，创建一个取消的操作结果。
        /// </param>
        public OperationResult(bool isSuccessOrCanceled)
        {
            Exception = null;
            IsCanceled = !isSuccessOrCanceled;
        }

        /// <summary>
        /// 获取一个值，该值指示操作已经成功完成。
        /// </summary>
        public bool Success => Exception == null && IsCanceled is false;

        /// <summary>
        /// 获取操作过程中发生或收集的异常。
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 获取此操作是否已被取消。
        /// </summary>
        public bool IsCanceled { get; }

        /// <summary>
        /// 将操作结果视为成功与否的 bool 值。
        /// </summary>
        public static implicit operator bool(OperationResult result) => result.Success;

        /// <summary>
        /// 将操作结果视为异常。
        /// </summary>
        public static implicit operator Exception(OperationResult result) => result.Exception;

        /// <summary>
        /// 将异常作为操作结果使用。
        /// </summary>
        public static implicit operator OperationResult(Exception exception)
            => exception is null ? null : new OperationResult(exception);

        /// <summary>
        /// 将成功或取消信息作为操作结果使用。
        /// </summary>
        public static implicit operator OperationResult(bool isSuccessOrCanceled)
            => new OperationResult(isSuccessOrCanceled);

        /// <summary>
        /// 判断操作是否是成功的。
        /// </summary>
        public static bool operator true(OperationResult result) => result.Success;

        /// <summary>
        /// 判断操作是否是失败的。
        /// </summary>
        public static bool operator false(OperationResult result) => !result.Success;
    }
}
