using System;
using System.Collections.Generic;
using System.Linq;

namespace dotnetCampus.Configurations.Utils
{
    internal static class CollectionExtensions
    {
        /// <summary>
        /// 在忽略顺序的情况下，比较两个集合是否相等（长度相等，包含的元素相等）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="list1">集合 1。</param>
        /// <param name="list2">集合 2。</param>
        /// <returns>如果两个集合忽略顺序后相等，则返回 true，否则返回 false。</returns>
        internal static bool SequenceEqualIgnoringOrder<T>(this ICollection<T> list1, ICollection<T> list2)
        {
            if (list1 == null)
            {
                throw new ArgumentNullException(nameof(list1));
            }

            if (list2 == null)
            {
                throw new ArgumentNullException(nameof(list2));
            }

            return list1.Count == list2.Count && list1.All(list2.Contains);
        }
    }
}
