namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 表示如何表示文件内容是否相等。
    /// </summary>
    internal enum FileEqualsComparison
    {
        /// <summary>
        /// 整个文件中有必须所有内容相同才视为相等。
        /// </summary>
        WholeTextEquals,

        /// <summary>
        /// 无视文件内容，只要新旧文件中包含的键值集合无序相等，则视为相等。
        /// </summary>
        KeyValueEquals,
    }
}
