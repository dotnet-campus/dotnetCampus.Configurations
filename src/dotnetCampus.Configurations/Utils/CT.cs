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
        /// 记一个日志。
        /// </summary>
        /// <param name="message">消息。</param>
        /// <param name="tags">消息标签（用于分类）。</param>
        //[Conditional("UNITTEST")]
        internal static void Log(string message, params string[] tags)
        {
            var text = $"[{DateTime.Now:O}] {string.Join(" ", tags.Select(x => $"[{x}]"))} {message}";
            Debug.WriteLine(text);
        }
    }
}
