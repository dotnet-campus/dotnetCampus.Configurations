namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 表示这是一个只读的配置仓库
    /// </summary>
    public interface IReadOnlyConfigurationRepo
    {
        /// <summary>
        /// 获取指定配置项的值，如果指定的 <paramref name="key"/> 不存在，则返回 null。
        /// 此方法是线程安全的。
        /// </summary>
        /// <param name="key">配置项的标识符。</param>
        /// <returns>配置项的值。</returns>
        string? GetValue(string key);
    }
}
