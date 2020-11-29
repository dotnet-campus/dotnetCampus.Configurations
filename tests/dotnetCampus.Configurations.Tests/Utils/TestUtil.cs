using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace dotnetCampus.Configurations.Tests.Utils
{
    /// <summary>
    /// 为需要清理的单元测试提供辅助工具。
    /// </summary>
    [TestClass]
    public static class TestUtil
    {
        private static readonly ConcurrentDictionary<string, FileInfo> AllTempFiles = new ConcurrentDictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取一个临时的用于测试的文件。
        /// </summary>
        /// <param name="extension">指定临时文件应该使用的扩展名。</param>
        /// <param name="templateFileName">
        /// 如果指定临时文件的模板，则会确保生成的临时文件存在且与模板文件相同；
        /// 如果指定临时文件的模板为 null，则仅会返回一个临时文件的路径，而不会创建文件。</param>
        /// <returns>用于测试的临时文件。</returns>
        public static FileInfo GetTempFile(string? templateFileName = null, string? extension = null)
        {
            var newFileName = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
            if (!string.IsNullOrWhiteSpace(templateFileName))
            {
                extension = extension ?? Path.GetExtension(templateFileName);
                newFileName += extension;
                File.Copy(templateFileName, newFileName);
            }
            else
            {
                newFileName += extension;
            }
            var fileInfo = new FileInfo(newFileName);
            AllTempFiles[newFileName] = fileInfo;
            return fileInfo;
        }

        [AssemblyCleanup]
        public static void CleanupTempFiles()
        {
            foreach (var file in AllTempFiles.Where(x => File.Exists(x.Key)))
            {
                file.Value.Delete();
            }
        }
    }
}
