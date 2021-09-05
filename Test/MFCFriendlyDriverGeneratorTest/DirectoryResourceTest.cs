using NUnit.Framework;
using System.IO;
using System.Linq;

namespace MFCFriendlyDriverGenerator {
    [TestFixture]
    public class DirectoryResourceTest {

        [Test]
        public void 一時ディレクトリ以下に個別のディレクトリが作成されているか() {
            using var dir = DirectoryResource.CreateTempDir();
            DirectoryAssert.Exists(dir.DirectoryPath);
            DirectoryAssert.AreEqual(
                new(Path.GetTempPath()),
                new(Path.GetFullPath(Path.Combine(dir.DirectoryPath, ".."))));
        }

        [Test]
        public void 複数個一時ディレクトリを作成でき個別に削除できるか() {
            var numOfCreation = 100;
            var directories = Enumerable.Range(0, numOfCreation)
                .Select(i => DirectoryResource.CreateTempDir())
                .ToDictionary(dir => dir.DirectoryPath);

            directories.Count.Is(numOfCreation);
            foreach (var pair in directories) {
                DirectoryAssert.Exists(pair.Key);
                pair.Value.Dispose();
                DirectoryAssert.DoesNotExist(pair.Key);
            }
            // すべて消えたか確認
            foreach (var pair in directories) {
                DirectoryAssert.DoesNotExist(pair.Key);
            }
        }

        [Test]
        public void 一時ディレクトリの中にファイルやディレクトリを作成し削除できるか() {
            var text = "this is test.";
            string filePath;
            {
                using var dir = DirectoryResource.CreateTempDir();
                filePath = Path.Combine(dir.DirectoryPath, "textfile.txt");
                // ファイルが作成できたことを確認
                File.WriteAllText(filePath, text);
                File.ReadAllText(filePath).Is(text);
                FileAssert.Exists(filePath);
            }
            FileAssert.DoesNotExist(filePath);
        }

        [Test]
        public void ディレクトリのパスが区切り文字で終了しているか() {
            using var dir = DirectoryResource.CreateTempDir();
            dir.DirectoryPath.Last().Is(Path.DirectorySeparatorChar);
        }
    }
}
