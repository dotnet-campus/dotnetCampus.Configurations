namespace dotnetCampus.Configurations.Concurrent
{
    /// <summary>
    /// 表示跨进程安全的值的状态。
    /// </summary>
    internal enum ProcessSafeValueState
    {
        /// <summary>
        /// 从外部（文件）读到值时，会保持此值。
        /// <list type="bullet">
        /// <item>被修改后会改为 <see cref="Changed"/></item>
        /// <item>被删除后会改为 <see cref="Deleted"/></item>
        /// </list>
        /// </summary>
        NotChanged = 0,

        ///// <summary>
        ///// 由此进程新建的值会保持此值。
        ///// <list type="bullet">
        ///// <item>与外部文件同步后会改为 <see cref="NotChanged"/></item>
        ///// <item>被删除后会改为 <see cref="Deleted"/></item>
        ///// <item>即使被修改，也不会改为 <see cref="Changed"/></item>
        ///// </list>
        ///// </summary>
        //New = 1,

        /// <summary>
        /// 由本进程修改过的值会保持此值。
        /// <list type="bullet">
        /// <item>与外部文件同步后会改为 <see cref="NotChanged"/></item>
        /// <item>被删除后会改为 <see cref="Deleted"/></item>
        /// </list>
        /// </summary>
        Changed = 2,

        /// <summary>
        /// 由本进程删除的值会保持此值。
        /// <list type="bullet">
        /// <item>与外部文件同步后会根据外部值是否改变决定是真实删除值还是读取外部值后改为 <see cref="NotChanged"/></item>
        /// <item>修改后会改为 <see cref="Changed"/></item>
        /// </list>
        /// </summary>
        Deleted = 3,
    }
}
