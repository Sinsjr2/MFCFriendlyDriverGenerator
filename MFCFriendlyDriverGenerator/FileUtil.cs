using System.IO;
using System.Text;
using System.Threading;
namespace MFCFriendlyDriverGenerator {
    public static class FileUtil {

        /// <summary>
        ///  ファイルの書き出しをキャンセルありで行います。
        ///  エンコーディングはデフォルトでUTF8 BOMなしです。
        /// </summary>
        public static void WriteAllText(string filePath, string text, CancellationToken token, Encoding? encoding = null)
        {
            using var stream = new StreamWriter(filePath, false, encoding ?? Encoding.UTF8);
            stream.WriteAsync(text).Wait(token);
        }
    }
}
