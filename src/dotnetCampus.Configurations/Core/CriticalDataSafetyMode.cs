using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 表示多个进程使用同一份数据的时候，将以什么安全级别来使用这份数据。
    /// </summary>
    public enum CriticalDataSafetyMode
    {
        /// <summary>
        /// 以不安全的方式来使用这份数据。
        /// <para>这能带来更快的数据读写性能，但可能在跨进程读写数据时带来少量的数据丢失。</para>
        /// <para>适用于单个进程管理自己的配置文件的情况，没有其他进程跟此进程竞争读写配置，这可以获得最佳性能。</para>
        /// <para>* * 简单来说 * *</para>
        /// <para>* * 就是这份配置就你一个程序用，没人管你。 * *</para>
        /// </summary>
        Unsafe,

        /// <summary>
        /// 优先以不安全的方式来使用这份数据。
        /// <para>这能在大多数时候带来更快的数据读写性能，但只要有任何一个进程需要以安全的方式来使用这份数据，那么此进程就会更改为安全的读写方式。</para>
        /// <para>适用于大多数场景，例如此程序管理的是自己的配置文件，但此配置也可能被其他进程少量读写。</para>
        /// <para>* * 简单来说 * *</para>
        /// <para>* * 就是这份配置一般就你一个程序用，其他程序只是偶尔掺和一下。 * *</para>
        /// </summary>
        UnsafeFirst,

        /// <summary>
        /// 以安全的方式来使用这份数据。
        /// <para>如果这份配置文件很少被竞争，那么性能不会降低太多；但如果此配置文件经常被竞争，那么可能因此而降低较多的性能；但牺牲此性能能保证数据在多个进程之间的正确性。</para>
        /// <para>如果某个配置文件经常被跨进程读写，并且依然要求配置文件的正确性，则应该使用此方案。</para>
        /// <para>* * 简单来说 * *</para>
        /// <para>* * 就是这份配置经常被多个进程共用，并且可能被误作简易的跨进程通信。 * *</para>
        /// </summary>
        Safe,

        /// <summary>
        /// 优先以安全的方式来使用这份数据。
        /// <para>此进程需要读写其他程序的配置文件，并且希望保证配置数据的正确性，除非对方程序强制以不安全的方式读写此配置。</para>
        /// <para>* * 简单来说 * *</para>
        /// <para>* * 就是你希望掺和一下别的程序的配置文件。 * *</para>
        /// </summary>
        SafeFirst,
    }
}
