using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 配置文件 Coin 序列化
    /// </summary>
    public static class CoinConfigurationSerializer
    {
        /// <summary>
        /// 存储的转义
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string EscapeString(string str)
        {
            // 如果开头是 `>` 就需要转换为 `?>`
            // 开头是 `?` 转换为 `??`

            var splitString = _splitString;
            var escapeString = _escapeString;

            if (str.StartsWith(splitString, StringComparison.Ordinal))
            {
                return _escapeString + str;
            }

            if (str.StartsWith(escapeString, StringComparison.Ordinal))
            {
                return _escapeString + str;
            }

            return str;
        }

        /// <summary>
        /// 将键值对字典序列化为文本字符串。
        /// </summary>
        /// <param name="keyValue">要序列化的键值对字典。</param>
        /// <returns>序列化后的文本字符串。</returns>
        public static string Serialize(IReadOnlyDictionary<string, string> keyValue)
        {
            if (ReferenceEquals(keyValue, null)) throw new ArgumentNullException(nameof(keyValue));
            var keyValuePairList = keyValue.ToArray().OrderBy(p => p.Key);

            return Serialize(keyValuePairList);
        }

        /// <summary>
        /// 将键值对字典序列化为文本字符串。
        /// </summary>
        /// <param name="keyValue">要序列化的键值对字典。</param>
        /// <returns>序列化后的文本字符串。</returns>
        public static string Serialize(Dictionary<string, string> keyValue)
        {
            if (ReferenceEquals(keyValue, null)) throw new ArgumentNullException(nameof(keyValue));
            var keyValuePairList = keyValue.ToArray().OrderBy(p => p.Key);

            return Serialize(keyValuePairList);
        }

        private static string Serialize(IOrderedEnumerable<KeyValuePair<string, string>> keyValuePairList)
        {
            var str = new StringBuilder();
            str.Append("> 配置文件\n");
            str.Append("> 版本 1.0\n");

            foreach (var temp in keyValuePairList)
            {
                // str.AppendLine 在一些地区使用的是 \r\n 所以不符合反序列化

                str.Append(EscapeString(temp.Key));
                str.Append("\n");
                str.Append(EscapeString(temp.Value));
                str.Append("\n>\n");
            }

            str.Append("> 配置文件结束");
            return str.ToString();
        }

        private static string _splitString = ">";
        private static string _escapeString = "?";

        /// <summary>
        /// 反序列化的核心实现，反序列化字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Dictionary<string, string> Deserialize(string str)
        {
            if (ReferenceEquals(str, null)) throw new ArgumentNullException(nameof(str));
            var keyValuePairList = str.Split('\n');
            var keyValue = new Dictionary<string, string>(StringComparer.Ordinal);
            string? key = null;
            var splitString = _splitString;

            foreach (var temp in keyValuePairList.Select(temp => temp.Trim()))
            {
                if (temp.StartsWith(splitString, StringComparison.Ordinal))
                {
                    // 分割，可以作为注释，这一行忽略
                    // 下一行必须是key
                    key = null;
                    continue;
                }

                var unescapedString = UnescapeString(temp);

                if (key == null)
                {
                    key = unescapedString;
                    
                    // 文件存在多个地方都记录相同的值
                    // 如果有多个地方记录相同的值，使用最后的值替换前面文件
                    if (keyValue.ContainsKey(key))
                    {
                        keyValue.Remove(key);
                    }
                }
                else
                {
                    if (keyValue.ContainsKey(key))
                    {
                        // key
                        // v1
                        // v2
                        // 返回 {"key","v1\nv2"}
                        keyValue[key] = keyValue[key] + "\n" + unescapedString;
                    }
                    else
                    {
                        keyValue.Add(key, unescapedString);
                    }
                }
            }

            return keyValue;
        }

        /// <summary>
        /// 存储的反转义
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string UnescapeString(string str)
        {
            var escapeString = _escapeString;

            if (str.StartsWith(escapeString, StringComparison.Ordinal))
            {
                return str.Substring(1);
            }

            return str;
        }
    }
}