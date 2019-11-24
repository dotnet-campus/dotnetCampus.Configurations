using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace dotnetCampus.Threading
{
    /// <summary>
    /// 对于异步的，出错后会重试的操作，使用此类型可以辅助等待循环重试的一部分。
    /// </summary>
    public class PartialAwaitableRetry
    {
        private readonly object _locker = new object();
        private readonly Func<PartialRetryContext, Task<OperationResult>> _loopItem;
        private readonly List<CountLimitOperationToken> _tokens = new List<CountLimitOperationToken>();
        private volatile bool _isLooping;

        /// <summary>
        /// 使用一个循环任务初始化 <see cref="PartialAwaitableRetry"/> 的一个新实例。
        /// </summary>
        /// <param name="loopItem">一个循环任务。</param>
        public PartialAwaitableRetry(Func<PartialRetryContext, Task<OperationResult>> loopItem)
        {
            _loopItem = loopItem ?? throw new ArgumentNullException(nameof(loopItem));
        }

        /// <summary>
        /// 以指定的次数限制加入循环，并返回等待此循环结果的可等待对象。
        /// 此方法是线程安全的。
        /// </summary>
        /// <param name="countLimit">次数限制，当设置为 -1 时表示无限次循环。</param>
        /// <returns>等待循环结果的可等待对象。</returns>
        public ContinuousPartOperation JoinAsync(int countLimit)
        {
            var token = new CountLimitOperationToken(countLimit);

            lock (_locker)
            {
                _tokens.Add(token);
                if (!_isLooping)
                {
                    Loop();
                }
            }

            return token.Operation;
        }

        /// <summary>
        /// 执行实际的循环，并在每一次执行的时候会给所有的等待对象报告结果。
        /// </summary>
        private async void Loop()
        {
            _isLooping = true;

            var context = new PartialRetryContext();
            var shouldContinue = true;

            try
            {
                while (shouldContinue)
                {
                    Exception exception;
                    bool isCompleted;

                    // 加锁获取此时此刻的 Token 集合副本。
                    // 执行一次循环的时候，只能操作此集合副本，真实集合新增的元素由于没有参与循环操作的执行；
                    // 这意味着期望执行一次方法的时候却没有执行，所以不能提供结果。
                    List<CountLimitOperationToken> snapshot;
                    lock (_locker)
                    {
                        snapshot = _tokens.ToList();
                    }

                    try
                    {
                        var result = await _loopItem.Invoke(context).ConfigureAwait(false);
                        exception = result.Exception;
                        isCompleted = result.Success;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        exception = ex;
                        isCompleted = false;
                    }

                    if (exception != null)
                    {
                        foreach (var token in snapshot)
                        {
                            token.UpdateException(exception);
                        }
                    }

                    if (isCompleted)
                    {
                        foreach (var token in snapshot)
                        {
                            token.Complete();
                        }

                        lock (_locker)
                        {
                            _tokens.RemoveAll(token => snapshot.Contains(token));
                            shouldContinue = _tokens.Count > 0;
                        }
                    }
                    else
                    {
                        foreach (var token in snapshot)
                        {
                            token.Pass(context.StepCount);
                        }
                    }
                }
            }
            finally
            {
                _isLooping = false;
            }
        }
    }

    /// <summary>
    /// 为 <see cref="PartialAwaitableRetry"/> 提供循环执行的上下文设置信息。
    /// </summary>
    public sealed class PartialRetryContext
    {
        private int _stepCount = 1;

        /// <summary>
        /// 获取或设置此方法一次执行时经过了多少次循环。
        /// 当某个方法执行时需要进行不打断的多次循环才能完成时，可以修改此值。
        /// </summary>
        public int StepCount
        {
            get => _stepCount;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("次数必须大于或等于 1。", nameof(value));
                }

                _stepCount = value;
            }
        }
    }
}
