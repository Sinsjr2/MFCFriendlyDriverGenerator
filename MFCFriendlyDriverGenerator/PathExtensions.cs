using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MFCFriendlyDriverGenerator {
    public static class PathExtensions {

        /// <summary>
        ///  現在実行しているOSでのパス比較方法
        /// </summary>
        static readonly StringComparison PathComparison =
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        /// <summary>
        ///  ディレクトリがサブディレクトリであるかを判定します。
        ///  <see cref="basePath"/>はディレクトリとして扱い、
        ///  <see cref="path"/は最後にパスの文字が入っていなければファイルとして扱います>ます。
        /// </summary>
        public static bool IsSubDirectory(this string basePath, string path) {
            var relativePath = basePath.EndWithDirSeparator().GetRelativePath(path);
            return !Path.IsPathRooted(relativePath) &&
                (relativePath == "" || !relativePath.StartsWith(".."));
        }

        /// <summary>
        /// ディレクトリの区切り文字で終わるように文字列を変換します。
        /// </summary>
        /// <returns></returns>
        public static string EndWithDirSeparator(this string path) {
            var delimiter = Path.DirectorySeparatorChar.ToString();
            return path.EndsWith(delimiter) ? path : path + delimiter;
        }

        /// <summary>
        ///  basePathを基準としてpathへの相対パスを取得します。
        ///  https://smdn.jp/programming/dotnet-samplecodes/filesystem/8a1b8349152c11eba787c72304437b56/を参考にしました。
        /// </summary>
        public static string GetRelativePath(this string basePath, string path) {
            if (basePath is null) {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (path is null) {
                throw new ArgumentNullException(nameof(path));
            }

            // 現在Windows上で動作しているかどうか
            var isRunningOnWindows = (int)Environment.OSVersion.Platform < 4;

            if (isRunningOnWindows && !Path.IsPathRooted(basePath)) {
                throw new ArgumentException("パスは絶対パスである必要があります", nameof(basePath));
            }

            // URIとして処理する前にパス中の%をURLエンコードする
            basePath = basePath.Replace("%", "%25");
            path     = path    .Replace("%", "%25");

            if (!isRunningOnWindows) {
                // 非Windows環境ではパスをURIとして解釈させるためにfile://スキームを前置する
                basePath = Uri.UriSchemeFile + Uri.SchemeDelimiter + basePath.Replace(":", "%3A");
                path     = Uri.UriSchemeFile + Uri.SchemeDelimiter + path    .Replace(":", "%3A");
            }

            // パスをURIに変換
            var uriBase = new Uri(basePath);
            var uriTarget = new Uri(path);

            // MakeRelativeUriメソッドで相対パスを取得する
            // 同時にURIに変換する際にエスケープされた文字列をアンエスケープする
            var relativePath = Uri.UnescapeDataString(uriBase.MakeRelativeUri(uriTarget).ToString())
                // ディレクトリ区切り文字を環境に合わせて置換する
                .Replace('/', Path.DirectorySeparatorChar);

            // URLエンコードした%を元に戻す
            return relativePath.Replace("%25", "%");
        }
    }
}
