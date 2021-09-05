using System;
using System.IO;

namespace MFCFriendlyDriverGenerator {

    /// <summary>
    ///  ディレクトリの中身とディレクトリ自身を破棄できます。
    /// </summary>
    public class DirectoryResource : IDisposable {

        /// <summary>
        ///  ディレクトリの絶対パスを返します。
        ///  ディレクトリの区切り文字で終了します。
        /// </summary>
        public string DirectoryPath { get; }

        /// <summary>
        ///  指定したディレクトリを作成します。
        /// </summary>
        public DirectoryResource(string path) {
            DirectoryPath = Path.GetFullPath(path);
            Directory.CreateDirectory(DirectoryPath);
        }

        public void Dispose() {
            if (Directory.Exists(DirectoryPath)){
                Directory.Delete(DirectoryPath, true);
            }
        }

        /// <summary>
        ///  一時ディレクトリにランダムな名前のディレクトリを作ります。
        /// </summary>
        public static DirectoryResource CreateTempDir() {
            return new DirectoryResource(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.DirectorySeparatorChar));
        }
    }
}
