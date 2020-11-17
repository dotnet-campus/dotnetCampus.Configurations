using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace dotnetCampus.Configurations.Utils
{
    internal static class OSUtils
    {
        /// <summary>
        /// 判断当前系统环境下两个路径是否是同一个路径。
        /// 注意，对于相对路径以及路径的 ./.. 跳转，即使最终对应同一个文件，也不被视为相同路径。
        /// </summary>
        /// <param name="path1">路径 1。</param>
        /// <param name="path2">路径 2。</param>
        /// <returns>如果当前系统环境下是同一个路径则返回 true，否则返回 false。</returns>
        internal static bool PathEquals(string path1, string path2)
        {
            return string.Equals(path1, path2,
                IsPathCaseSensitive() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断当前系统环境下路径是否是大小写敏感的。
        /// </summary>
        /// <returns>如果当前系统环境下路径大小写敏感则返回 true，否则返回 false。</returns>
        internal static bool IsPathCaseSensitive()
        {
#if NETFRAMEWORK
            return false;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return false;
            }
#endif
            return true;
        }
    }
}
