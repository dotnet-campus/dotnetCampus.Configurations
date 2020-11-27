namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 表示如何识别文件是否改变。
    /// </summary>
    internal enum ChangeDetectionType
    {
        /// <summary>
        /// 整个文件中有必须所有内容相同才视为相同。
        /// </summary>
        WholeTextEquals,

        /// <summary>
        /// 无视文件内容，只要新旧文件中包含的键值集合无序相等，则视为相同。
        /// </summary>
        KeyValueEquals,
    }
}
