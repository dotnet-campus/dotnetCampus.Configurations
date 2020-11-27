using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace dotnetCampus.Configurations.Core
{
    /// <summary>
    /// 被注释的值。
    /// </summary>
    [DebuggerDisplay("{Value,nq} // {Comment,nq}")]
    public readonly struct CommentedValue<T> : IEquatable<CommentedValue<T>>
    {
        /// <summary>
        /// 值。
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// 注释。
        /// </summary>
        public string Comment { get; }

        public CommentedValue(T value, string comment = "")
        {
            Value = value;
            Comment = comment ?? throw new ArgumentNullException(nameof(comment));
        }

        public override bool Equals(object? obj)
        {
            return obj is CommentedValue<T> value && Equals(value);
        }

        public bool Equals(CommentedValue<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value)
                && string.Equals(Comment, other.Comment, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            var hashCode = 618185720;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(Comment);
            return hashCode;
        }

        public static bool operator ==(CommentedValue<T> left, CommentedValue<T> right) => left.Equals(right);

        public static bool operator !=(CommentedValue<T> left, CommentedValue<T> right) => !(left == right);
    }
}
