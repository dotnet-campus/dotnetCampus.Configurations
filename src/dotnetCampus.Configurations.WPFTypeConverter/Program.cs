#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Windows;

namespace dotnetCampus.Configurations.WPFTypeConverter
{
    /// <summary>
    /// 提供 Configuration 转换 WPF 的 <see cref="Size"/> 的方法
    /// </summary>
    public static class SizeConverter
    {
        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="value"></param>
        /// <param name="key"></param>
        public static void SetValue(this Configuration configuration, Size? value,
            [CallerMemberName] string key = "")
        {
            var sizeText = Serialize(value);
            configuration.SetValue(sizeText, key);
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Size? GetSize(this Configuration configuration,
            [CallerMemberName] string key = "")
        {
            var sizeText = configuration.GetValue(key);

            try
            {
                return Deserialize(sizeText);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"存储Size的值不合法 key={key} value={sizeText}", e);
            }
        }

        private static string Serialize(Size? value)
        {
            return value.HasValue ? $"{value.Value.Width};{value.Value.Height}" : string.Empty;
        }

        private static Size? Deserialize(string? sizeText)
        {
            if (string.IsNullOrEmpty(sizeText))
            {
                return null;
            }

            string size = sizeText!;

            var n = size.IndexOf(';');

            var width = size.Substring(0, n);
            var height = size.Substring(n + 1, size.Length - n - 1);

            return new Size(double.Parse(width), double.Parse(height));
        }
    }
}