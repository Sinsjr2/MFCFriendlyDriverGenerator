using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MFCFriendlyDriverGenerator {

    /// <summary>
    ///  コマンドを実行した時の例外
    ///  エラーメッセージと終了コードを持っています。
    /// </summary>
    public class CommandRunException : Exception {
        public int ExitCode { get; }
        public string ErrorMessage { get; }

        public CommandRunException(int exitCode, string errorMessage) :
            base($"Exit Code : {exitCode}, Message: " + errorMessage) {
            ExitCode = exitCode;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    ///  コマンドを実行した時の結果
    /// </summary>
    public record ProcessResult(string StandardOutput, string StandardError);

    public static class ProcessExtensions {

        /// <summary>
        ///  コマンドを実行し、標準出力と標準エラー出力を返します。
        ///  コマンドを実行した時に終了コードが0で無かった場合は例外が発生します。
        ///  作業ディレクトリが指定されていない場合(nullもしくは空文字列)はカレントディレクトリを作業ディレクトリにします。
        ///  また、作業ディレクトリが存在しない場合は例外が発生します。
        /// </summary>
        public static ProcessResult Run(this string command, string arguments, string? workingDir = null, Encoding? outputEncoding = null, Encoding? errorEncoding = null) {
            // 存在しないディレクトリでコマンドを実行した場合
            // 例外は発生するがメッセージがわかりにくいためチェックする
            var workingDirectory = string.IsNullOrEmpty(workingDir)
                ? Environment.CurrentDirectory
                : workingDir;
            if (!Directory.Exists(workingDirectory)) {
                throw new DirectoryNotFoundException(workingDirectory);
            }

            using var process = Process.Start(
                new ProcessStartInfo(command, arguments) {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory,
                    StandardOutputEncoding = outputEncoding,
                    StandardErrorEncoding = errorEncoding,
                }
            );

            var outAndErrorTask = Task.WhenAll(process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync());
            process.WaitForExit();
            var outAndErrorStr = outAndErrorTask.Result;

            return process.ExitCode == 0
                ? new ProcessResult(outAndErrorStr[0], outAndErrorStr[1])
                : throw new CommandRunException(process.ExitCode, outAndErrorStr[1]);
        }
    }
}
