using System.IO;
using System.Linq;
using NUnit.Framework;

namespace MFCFriendlyDriverGenerator {
    [TestFixture]
    public class PathExtensionsTest {

        /// <summary>
        /// /区切りを実行中のosの区切り文字に変換します。
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        string ToCurrentOSPath(string path) {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        [Test]
        [TestCase(@"user1\data\", @"c:\users\", @"c:\users\user1\data\")]
        [TestCase(@"..\..\", @"c:\home\user1\data\", @"c:\home\")]
        [TestCase(@"hoge", @"c:\", @"c:\hoge")]
        [TestCase(@"", @"c:\foo", @"c:\foo")]
        [TestCase(@"..\%piyo%%", @"c:\foo\hoge\", @"c:\foo\%piyo%%")]
        public void GetRelativePathTest(string expected, string @base, string path) {
            @base.GetRelativePath(path).Is(expected);
        }

        [Test]
        [TestCase(@"C:\hoge\foo\", @"C:\foo\", false)]
        [TestCase(@"C:\hoge\foo\", @"C:\hoge\", false)]
        [TestCase(@"C:\hoge\foo\", @"C:\hoge\foo\", true)]
        [TestCase(@"C:\hoge\foo\", @"C:\hoge\foo\", true)]
        [TestCase(@"C:\root\foo\", @"C:\root\foo\hoge\", true)]
        [TestCase(@"C:\root\foo\", @"C:\root\foo\hoge", true)]
        [TestCase(@"C:\root\foo\", @"C:\root\foo\hoge\bar", true)]
        [TestCase(@"C:\root\foo\", @"C:\hoge\foo\", false)]
        [TestCase(@"C:\root\", @"Z:\root\hoge.txt", false)]
        public void IsSubDirectoryTest(string basePath, string path, bool isSubDir) {
            basePath.IsSubDirectory(path).Is(isSubDir);
        }
    }
}
