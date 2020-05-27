#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#pragma warning disable CA1033

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 提供一个异步 <see cref="Configuration"/> 配置管理仓库的基类。
    /// </summary>
    public abstract class AsynchronousConfigurationRepo : IConfigurationRepo
    {
        /// <summary>
        /// 创建一个使用强类型的用于提供给应用程序业务使用的应用程序配置管理器。
        /// </summary>
        /// <returns>用于提供给应用程序业务使用的配置管理器。</returns>
        public IAppConfigurator CreateAppConfigurator() => new ConcurrentAppConfigurator(this);

        /// <summary>
        /// 获取指定配置项的值，如果指定的 <paramref name="key"/> 不存在，则返回 null。
        /// 此方法是线程安全的。
        /// </summary>
        /// <param name="key">配置项的标识符。</param>
        /// <returns>配置项的值。</returns>
        string? IConfigurationRepo.GetValue(string key)
        {
            VerifyKey(key);

            return TryReadAsync(key).Result;
        }

        /// <summary>
        /// 设置指定配置项的值，如果设置为 null，可能删除 <paramref name="key"/> 配置项。
        /// 此方法是线程安全的。
        /// </summary>
        /// <param name="key">配置项的标识符。</param>
        /// <param name="value">配置项的值。</param>
        void IConfigurationRepo.SetValue(string key, string? value)
        {
            VerifyKey(key, true);

            WriteAsync(key, value).Wait();
        }

        /// <summary>
        /// 删除所有满足 <paramref name="keyFilter"/> 规则的 Key 所表示的配置项。
        /// </summary>
        /// <param name="keyFilter">
        /// 指定如何过滤 Key。当指定为 null 时，全部清除。
        /// </param>
        void IConfigurationRepo.ClearValues(Predicate<string> keyFilter)
        {
            keyFilter = keyFilter ?? (_ => true);
            var keys = GetKeys();
            if (keys == null)
            {
                throw new InvalidOperationException($"重写 {nameof(GetKeys)} 方法时，不应该返回 null，而应该返回空集合。");
            }

            RemoveKeys().Wait();

            async Task RemoveKeys()
            {
                var isChanged = false;
                foreach (var key in keys.Where(keyFilter.Invoke))
                {
                    isChanged = true;
                    await RemoveValueCoreAsync(key).ConfigureAwait(false);
                }

                if (isChanged)
                {
                    // 通知子类处理值的改变，并收集改变后果。
                    // 此部分内容不属于 ClearValues，所以不等待。
#pragma warning disable 4014
                    NotifyChanged(keys);
#pragma warning restore 4014
                }
            }
        }

        /// <summary>
        /// 尝试读取配置文件信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="default">如果无法读取到信息，默认返回的值</param>
        /// <returns></returns>
        public async Task<string> TryReadAsync(string key, string @default = "")
        {
            if (@default == null)
            {
                throw new ArgumentNullException(nameof(@default));
            }

            VerifyKey(key);

            var value = await ReadValueCoreAsync(key).ConfigureAwait(false);
            return value ?? @default;
        }

        /// <summary>
        /// 写入配置文件信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public async Task WriteAsync(string key, string? value)
        {
            VerifyKey(key, true);

            // 如果值没有变化，则不做任何处理。
            var originalValue = await TryReadAsync(key).ConfigureAwait(false);
            if (originalValue.Equals(value, StringComparison.InvariantCulture))
            {
                return;
            }

            // 由子类处理值的改变。
            if (value is null || string.IsNullOrEmpty(value))
            {
                await RemoveValueCoreAsync(key).ConfigureAwait(false);
            }
            else
            {
                await WriteValueCoreAsync(key, value).ConfigureAwait(false);
            }

            // 通知子类处理值的改变，并收集改变后果。
            // 此部分内容不属于 WriteAsync，所以不等待。
#pragma warning disable 4014
            NotifyChanged(new[] { key });
#pragma warning restore 4014
        }

        private async Task NotifyChanged(IEnumerable<string> keys)
        {
            var context = new AsynchronousConfigurationChangeContext(keys);
            OnChanged(context);

            var asyncAction = context.GetTrackedAction();
            if (asyncAction != null)
            {
                await asyncAction.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 派生类重写此方法时，返回所有目前已经存储的 Key 的集合。
        /// </summary>
        protected abstract ICollection<string> GetKeys();

        /// <summary>
        /// 派生类重写此方法时，返回指定 Key 的值，如果不存在，需要返回 null。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        /// <returns>
        /// 执行项的 Key，如果不存在，则为 null / Task&lt;string&gt;.FromResult(null)"/>。
        /// </returns>
        protected abstract Task<string?> ReadValueCoreAsync(string key);

        /// <summary>
        /// 派生类重写此方法时，将为指定的 Key 存储指定的值。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        /// <param name="value">要存储的值。</param>
        protected abstract Task WriteValueCoreAsync(string key, string value);

        /// <summary>
        /// 派生类重写此方法时，将为指定的 Key 清除。
        /// </summary>
        /// <param name="key">指定项的 Key。</param>
        protected abstract Task RemoveValueCoreAsync(string key);

        /// <summary>
        /// 派生类重写此方法时，可以考虑将配置进行持久化。
        /// </summary>
        protected abstract void OnChanged(AsynchronousConfigurationChangeContext context);

        /// <summary>
        /// 为 <see cref="OnChanged"/> 事件提供异步追踪参数。
        /// </summary>
        protected sealed class AsynchronousConfigurationChangeContext
        {
            internal AsynchronousConfigurationChangeContext(IEnumerable<string> changedKeys)
            {
                changedKeys = changedKeys ?? throw new ArgumentNullException(nameof(changedKeys));
                if (changedKeys is IReadOnlyCollection<string> rc)
                {
                    ChangedKeys = rc;
                }
                else
                {
                    ChangedKeys = changedKeys.ToArray();
                }
            }

            /// <summary>
            /// 获取引起此上下文改变事件的配置项的 Key。
            /// </summary>
            public IReadOnlyCollection<string> ChangedKeys { get; }

            /// <summary>
            /// 要求配置的保存过程跟踪此异步操作，使得后续的状态处理必须在此异步操作结束之后才能执行。
            /// </summary>
            /// <param name="action">异步操作。</param>
            public void TrackAsyncAction(Task action) =>
                _trackedAction = action ?? throw new ArgumentNullException(nameof(action));

            /// <summary>
            /// 获取此对象储存的跟踪的异步操作。
            /// </summary>
            public Task? GetTrackedAction() => _trackedAction;

            private Task? _trackedAction;
        }

        /// <summary>
        /// 验证字符串 <paramref name="key"/> 能否成为配置项的键。
        /// </summary>
        /// <param name="key">配置项的键。</param>
        /// <param name="createNew">
        /// 如果调用此验证方法时，你需要使用这个键创建一个新的配置项（而不是读取原有配置），那么需要在这里传入 true。
        /// 当设为 true 时，此方法会额外验证键是否是单行的（不带换行符）。
        /// </param>
        [ContractArgumentValidator, MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void VerifyKey(string key, bool createNew = false)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("不允许使用空字符串作为配置项的 Key。", nameof(key));
            }

            if (createNew && (key.Contains('\n') || key.Contains('\r')))
            {
                throw new ArgumentException("不允许使用换行符串作为配置项的 Key。", nameof(key));
            }

            Contract.EndContractBlock();
        }
    }
}
