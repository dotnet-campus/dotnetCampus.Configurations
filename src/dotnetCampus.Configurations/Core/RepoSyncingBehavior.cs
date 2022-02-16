namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 表示配置仓库应如何与外部数据进行同步。
    /// </summary>
    public enum RepoSyncingBehavior
    {
        /// <summary>
        /// 以高效的方式进行同步。
        /// </summary>
        /// <remarks>
        /// 使用此方式会导致仓库监听外部文件的改变。
        /// </remarks>
        Sync,

        /// <summary>
        /// 不进行同步，首次读到值后将不再自动根据外部值更新数据，除非手动调用方法更新数据。
        /// </summary>
        Static,
    }
}
