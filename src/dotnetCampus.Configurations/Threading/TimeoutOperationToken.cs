using System;

// ReSharper disable once CheckNamespace
namespace dotnetCampus.Threading
{
    /// <summary>
    /// 为超时的异步等待操作提供操作。
    /// </summary>
    public class TimeoutOperationToken
    {
        private readonly TimeSpan _timeout;
        private TimeSpan _passed;

        /// <summary>
        /// 创建一个具有指定超时时间的 <see cref="TimeoutOperationToken"/>。
        /// </summary>
        /// <param name="timeout">超时时间，可能不是精确的。</param>
        public TimeoutOperationToken(TimeSpan timeout)
        {
            Operation = new ContinuousPartOperation();
            _timeout = timeout < TimeSpan.Zero ? TimeSpan.MaxValue : timeout;
        }

        /// <summary>
        /// 获取一个可 await 等待的等待对象。
        /// </summary>
        public ContinuousPartOperation Operation { get; }

        /// <summary>
        /// 完成此异步操作。
        /// </summary>
        /// <param name="removeIntermediateExceptions">
        /// 默认情况下，如果此前发生过异常，则认为那是重试过程中的中间异常，现在成功完成了任务，所以中间异常需要移除。
        /// 不过，你也可以选择不移除，意味着此任务的完成属于强制终止，而不是成功完成。
        /// </param>
        public void Complete(bool removeIntermediateExceptions = true)
        {
            if (removeIntermediateExceptions)
            {
                Operation.CleanException();
            }

            if (!Operation.IsCompleted)
            {
                Operation.Complete();
            }
        }

        /// <summary>
        /// 通知此 <see cref="ContinuousPartOperation"/> 自上次调用 <see cref="Pass"/> 方法以来经过的时间。
        /// </summary>
        /// <param name="passedTimeSinceLastPass">自上次调用 <see cref="Pass"/> 方法以来经过的时间。</param>
        public void Pass(TimeSpan passedTimeSinceLastPass)
        {
            _passed += passedTimeSinceLastPass;
            if (_passed >= _timeout && !Operation.IsCompleted)
            {
                Operation.Complete();
            }
        }

        /// <summary>
        /// 使用一个新的 <paramref name="exception"/> 来设置此异步操作完成对象。
        /// </summary>
        /// <param name="exception">一个异常，当设置后，同步或异步等待此对象时将抛出异常。</param>
        public void UpdateException(Exception exception)
        {
            if (!Operation.IsCompleted)
            {
                Operation.UpdateException(exception);
            }
        }
    }
}
