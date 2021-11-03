using System.Collections.Generic.Immutable;
using System.Threading;

namespace MFCFriendlyDriverGenerator {

    public record PreprocessResult(EqList<string> RefFilePaths, string PrecompiledCode);

    public interface IRcFilePreCompile {

        /// <summary>
        ///  引数で指定したRCファイルをプリプロセスします。
        ///  インクルードしているファイルには引数で渡したファイルは含まれません。
        ///  インクルードパスは絶対パスを返します。
        ///  戻り値のコードにディレクティブが入っていてもよい。
        /// </summary>
        /// <param name="info"> コマンドを実行してプリプロセスするための情報 </param>
        PreprocessResult Preprocess(PreprocessProcInfo info, string rcFilePath, CancellationToken token);
    }
}
