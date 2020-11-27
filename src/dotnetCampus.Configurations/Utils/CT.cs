using System;
using System.Diagnostics;
using System.Linq;

namespace dotnetCampus.Configurations.Utils
{
    /// <summary>
    /// 配置组件专用的日志记录器。
    /// </summary>
    internal static class CT
    {
        /// <summary>
        /// 获取或设置当前已设置的自定义输出重定向器。
        /// </summary>
        internal static Action<string>? OutputRedirector { get; set; }

        /// <summary>
        /// 记一个日志。
        /// </summary>
        /// <param name="message">消息。</param>
        /// <param name="tags">消息标签（用于分类）。</param>
        //[Conditional("UNITTEST")]
        internal static void Log(string message, params string[] tags)
        {
            var text = $"[{DateTime.Now:O}] {string.Join(" ", tags.Select(x => $"[{x}]"))} {message}";
            if (OutputRedirector is null)
            {
                Debug.WriteLine(text);
            }
            else
            {
                OutputRedirector(text);
            }
        }
    }
}
