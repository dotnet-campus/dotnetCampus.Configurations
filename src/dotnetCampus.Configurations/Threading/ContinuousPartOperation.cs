using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CA1034

// ReSharper disable once CheckNamespace
namespace dotnetCampus.Threading
{
    /// <summary>
    /// 为一个持续操作中的一部分提供可异步等待的操作。
    /// </summary>
    public class ContinuousPartOperation
    {
        private readonly TaskCompletionSource<object?> _source;
        private readonly Awaiter _awaiter;
        private Action? _continuation;
        private Exception? _exception;

        internal ContinuousPartOperation()
        {
            _source = new TaskCompletionSource<object?>();
            _awaiter = new Awaiter(this);
        }

        /// <summary>
        /// 获取一个值，该值指示此异步操作是否已经结束。
        /// </summary>
        internal bool IsCompleted { get; private set; }

        /// <summary>
        /// 完成此异步操作。
        /// </summary>
        internal void Complete()
        {
            IsCompleted = true;
            if (_exception == null)
            {
                _source.SetResult(null);
            }
            else
            {
                _source.SetException(_exception);
            }

            var continuation = _continuation;
            _continuation = null;
            continuation?.Invoke();
        }

        /// <summary>
        /// 使用一个新的 <paramref name="exception"/> 来设置此异步操作完成对象。
        /// </summary>
        /// <param name="exception">一个异常，当设置后，同步或异步等待此对象时将抛出异常。</param>
        internal void UpdateException(Exception exception)
            => _exception = exception ?? throw new ArgumentNullException(nameof(exception));

        /// <summary>
        /// 清除此异步等待操作中使用 <see cref="UpdateException"/> 设置过的异常。
        /// 这样，异步等待的类型在等待结束时不会引发一个异常。
        /// </summary>
        internal void CleanException() => _exception = null;

        /// <summary>
        /// 获取一个用于等待此异步操作的可等待对象。
        /// </summary>
        public Awaiter GetAwaiter() => _awaiter;

        /// <summary>
        /// 同步等待此异步操作完成。
        /// </summary>
        public void Wait() => _source.Task.GetAwaiter().GetResult();

        /// <summary>
        /// 表示用于等待 <see cref="ContinuousPartOperation"/> 的异步可等待对像。
        /// </summary>
        public sealed class Awaiter : INotifyCompletion
        {
            private readonly ContinuousPartOperation _owner;

            /// <summary>
            /// 创建一个用于等待 <see cref="ContinuousPartOperation"/> 的异步可等待对象。
            /// </summary>
            internal Awaiter(ContinuousPartOperation owner)
            {
                _owner = owner;
            }

            /// <summary>Schedules the continuation action that's invoked when the instance completes.</summary>
            /// <param name="continuation">The action to invoke when the operation completes.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="continuation">continuation</paramref> argument is null (Nothing in Visual Basic).</exception>
            public void OnCompleted(Action continuation)
            {
                if (IsCompleted)
                {
                    continuation?.Invoke();
                }
                else
                {
                    _owner._continuation += continuation;
                }
            }

            /// <summary>
            /// 获取一个值，该值指示异步操作是否完成。
            /// </summary>
            public bool IsCompleted => _owner.IsCompleted;

            /// <summary>
            /// 获取此异步操作的结果。
            /// </summary>
            [DebuggerStepThrough]
            public void GetResult() => _owner._source.Task.GetAwaiter().GetResult();
        }
    }
}
