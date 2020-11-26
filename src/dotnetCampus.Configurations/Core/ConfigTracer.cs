#pragma warning disable CA1810 // Initialize reference type static fields inline

using System;
using System.Diagnostics;
using System.Linq;

namespace dotnetCampus.Configurations.Core
{
    internal class ConfigTracer
    {
        [Conditional("TRACETAG")]
        internal static void Debug(string message, params string[] tags)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:O}] {string.Join(" ", tags.Select(x => $"[{x}]"))} {message}");
        }
    }
}
