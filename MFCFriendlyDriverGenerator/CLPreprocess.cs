using System.Collections.Generic;
using System.Collections.Generic.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace MFCFriendlyDriverGenerator {

    /// <summary>
    ///  CL.exeを用いてプリプロセスを行います。
    /// </summary>
    public class CLPreprocess : IRcFilePreCompile {

        /// <summary>
        ///  以下の様のような文字列から２つ目の:以降のパスを抜き出します。
        ///  hoge.rc
        ///  メモ: インクルード ファイル:  c:\Hoge\\resource.h
        /// </summary>
        IEnumerable<string> GetHeaderFilePaths(string processOutput) {
            foreach (Match match in Regex.Matches(processOutput, @"^[^:]*:[^:]*:[\t ]*(.*)$", RegexOptions.Multiline)) {
                yield return match.Groups[1].Value.Replace("\r", "");
            }
        }

        /// <summary>
        ///  rcファイルのプリプロセスを行います。
        ///  インクルードしているファイルには自身のファイルは含みません。
        /// </summary>
        public PreprocessResult Preprocess(PreprocessProcInfo info, string rcFilePath) {
            var defines = info.Defines.Concat(new[] { "RC_INVOKED", "WINAPI_PARTITION_DESKTOP=0" });
            var result = "cl".Run(
                string.Join(" ", info.IncludePaths.Select(path => $"/I\"{path}\"")) + " " +
                string.Join(" ", defines.Select(d => "/D" + d)) + " " +
                $"/EP /showIncludes /nologo /C \"{rcFilePath}\"", info.ProjectDir,
                // コメントが文字化けを防止する対策
                // コメント以外はshift_jisであるため、日本語が文字化けするが
                // コメントに重要な情報が詰まっているので、よしとする。
                outputEncoding : System.Text.Encoding.UTF8);

            return new PreprocessResult(
                GetHeaderFilePaths(result.StandardError).ToEqList(),
                result.StandardOutput);
        }
    }
}
