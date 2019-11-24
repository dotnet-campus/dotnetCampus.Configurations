using System;

// ReSharper disable once CheckNamespace
namespace dotnetCampus.Threading
{
    /// <summary>
    /// 为次数限制的异步等待操作提供操作。
    /// </summary>
    public class CountLimitOperationToken
    {
        private readonly long _countLimit;
        private long _passed;

        /// <summary>
        /// 创建一个具有指定执行次数限制的 <see cref="CountLimitOperationToken"/>。
        /// </summary>
        /// <param name="countLimit">次数限制，可能是不精确的。</param>
        public CountLimitOperationToken(long countLimit)
        {
            Operation = new ContinuousPartOperation();
            _countLimit = countLimit < 0 ? long.MaxValue : countLimit;
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
        /// 通知此 <see cref="ContinuousPartOperation"/> 自上次调用 <see cref="Pass"/> 方法以来增加的次数。
        /// </summary>
        /// <param name="countPassed">自上次调用 <see cref="Pass"/> 方法以来增加的次数。</param>
        public void Pass(long countPassed)
        {
            _passed += countPassed;
            if (_passed >= _countLimit && !Operation.IsCompleted)
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
