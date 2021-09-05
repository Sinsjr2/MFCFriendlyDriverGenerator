using System.Collections.Generic;
using System.Collections.Generic.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MFCFriendlyDriverGenerator {

    public class GccPreprocess // : IRcFilePreCompile
    {

        /// <summary>
        ///  以下の様のような文字列から.の行を抜き出します。
        ///  . Resources/hoge.h
        ///  . /home/user/afxres.h
        ///  Multiple include guards may be useful for:
        ///    Resources/hoge.h
        /// </summary>
        IEnumerable<string> GetHeaderFilePaths(string processOutput) {
            foreach (Match match in Regex.Matches(processOutput, @"^\. (.*)$", RegexOptions.Multiline)) {
                yield return match.Groups[1].Value.Replace("\r", "");
            }
        }

        public PreprocessResult Preprocess(PreprocessProcInfo info, string rcFilePath) {
            var result = "gcc".Run(
                "-finput-charset=UTF-16 -H -P -nostdinc " +
                string.Join(" ", info.Defines.Select(def => "-D" + def)) + " " +
                string.Join(" ", info.IncludePaths.Select(path => $"-I \"{path}\"")) + " " +
                " -E -x c-header -C " + rcFilePath, info.ProjectDir);

            return new PreprocessResult(
                GetHeaderFilePaths(result.StandardError)
                .Select(path => Path.Combine(info.ProjectDir, path)).ToEqList(),
                result.StandardOutput);
        }

    }
}
