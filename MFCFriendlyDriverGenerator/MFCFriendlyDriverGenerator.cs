using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sprache;
using Microsoft.CodeAnalysis;
using System;
using System.Xml.Serialization;
using MFCFriendlyDriverGenerator.Setting;
using System.Collections.Generic.Immutable;
using System.CodeDom.Compiler;
using System.Text;
using Hnx8.ReadJEnc;

namespace MFCFriendlyDriverGenerator {

    /// <summary>
    ///  プリプロセス処理に必要な情報
    /// </summary>
    public record PreprocessProcInfo(string ProjectDir, IReadOnlyList<string> Defines, IReadOnlyList<string> IncludePaths);

    [Generator]
    public class MFCFriendlyDriverGenerator : ISourceGenerator {

        /// <summary>
        ///  RCファイルのパースからドライバコードの生成に関する設定ファイル
        /// </summary>
        readonly string settingFileName = "mfc_friendly_gen.xml";

        /// <summary>
        ///  コード生成し、保存する時のファイル名
        /// </summary>
        readonly string generatedBaseFilename = "MFCFriendlyDriver";

        /// <summary>
        ///  ツールの名前とバージョン
        /// </summary>
        readonly GeneratedCodeAttribute toolInfo = new("MFCFriendlyDriverGenerator", "0.8.22");

        readonly IRcFilePreCompile preCompile;

        static MFCFriendlyDriverGenerator() {
            //.net5でshift_jisを使うための設定
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public MFCFriendlyDriverGenerator() {
            preCompile = new CLPreprocess();
        }

        public MFCFriendlyDriverGenerator(IRcFilePreCompile preCompile) {
            this.preCompile = preCompile;
        }

        /// <summary>
        ///  プロジェクトディレクトリ以下でincludeしているファイルのパスを返します。
        /// </summary>
        IEnumerable<string> GetRefFileInSubDir(PreprocessProcInfo info, string rcFilePath) {
            var precompiled = preCompile.Preprocess(info, rcFilePath);
            return precompiled.RefFilePaths
                .Select(path => {
                    return Path.GetFullPath(Regex.Replace(path, "/|\\\\", Path.DirectorySeparatorChar.ToString(), RegexOptions.Multiline));
                })
                .Where(path => PathExtensions.IsSubDirectory(info.ProjectDir, path));
        }

        (EqList<string> FileTextInProject, string PreCompiledString) Precompile(PreprocessProcInfo info, string rcFilePath) {
            var refFiles = GetRefFileInSubDir(info, rcFilePath)
                .Select(path => info.ProjectDir.EndWithDirSeparator().GetRelativePath(path))
                .ToArray();

            // 書き換えるファイルを一時ディレクトリにコピし、
            // defineを展開しないようにディレクティブ以外をコメントアウトする

            var escapeStr = "///== ";
            using var tempDir = DirectoryResource.CreateTempDir();

            var loadedFiles = new List<string>();

            foreach (var path in refFiles.Append(rcFilePath)) {
                var srcFile = new FileInfo(Path.Combine(info.ProjectDir, path));
                using var reader = new FileReader(srcFile);
                var charCode = reader.Read(srcFile);
                if (charCode is not CharCode.Text || reader.Text is null) {
                    throw new InvalidDataException($"invalid file or encoding. actual:{charCode} filename: {srcFile.FullName}");
                }
                var text = reader.Text;
                var code = PreprocessorDirectiveParser.TextLineRegex
                    .Replace(text, escapeStr + "$1");
                var outputFile = Path.Combine(tempDir.DirectoryPath, path);
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
                // 読みだしたエンコーディングと同じエンコーディングで書き戻す
                File.WriteAllText(outputFile, code, charCode.GetEncoding());
                loadedFiles.Add(text);
            }

            // コメントアウトしたコードを再度コンパイルし、コメントアウトを解除
            var rcString = string.Join(
                "",
                Regex.Matches(preCompile.Preprocess(info with { ProjectDir = tempDir.DirectoryPath }, rcFilePath).PrecompiledCode,
                              $"^{escapeStr}(.*(\n|\r|\r\n))", RegexOptions.Multiline)
                .Cast<Match>()
                .Select(match => match.Groups[1]));
            // ディレクティブを消す
            return (loadedFiles.ToEqList(), PreprocessorDirectiveParser.DirectiveRegex.Replace(rcString, ""));
        }

        /// <summary>
        ///  プリプロセス後の内容をdefineで定義された値に置き換えていない状態で返します。
        ///  また、ディレクトティブはすべて除いた文字列にします。
        ///  指定したプロジェクトのサブディレクトリにあるファイルのみdefineを展開しません。
        ///  rcファイルのパスは作業ディレクトリからの相対パスで指定している必要があります。
        /// </summary>
        public string PrecompiledString(PreprocessProcInfo info, string rcFilePath) {

            return Precompile(info, rcFilePath).PreCompiledString;
        }

        /// <summary>
        ///  指定された文字列の中のdefineを取得し、整数で記述されたリテラルのみを返します。
        ///  プリプロセッサの条件分岐はせずに単純にdifineと書かれている行を検索して返します。
        ///  キーはdefineの名前です。
        /// </summary>
        IEnumerable<KeyValuePair<string, int>> GetDefineConstValues(string text) {
            var defs = PreprocessorDirectiveParser.PreprocessorDirective
                .End()
                .Parse(text)
                .OfType<Define>();
            return
                from def in defs
                let maybeInt = ExpParser.IntLiteral.Token().Optional().Parse(def.Value)
                where maybeInt.IsDefined
                select new KeyValuePair<string, int>(def.Name, maybeInt.Get());
        }

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context) {
            // 設定ファイルは1以下でないといけない
            var xmlFiles = context.AdditionalFiles.Where(at => Path.GetFileName(at.Path) == settingFileName).ToList();
            if (1 < xmlFiles.Count) {
                Console.Error.WriteLine("too many setting files: " + string.Join("\n", xmlFiles) + "\n" +
                                    "expected 0 or 1.");
                return;
            }
            foreach (var xmlFile in xmlFiles) {
                ProcessSettingFile(xmlFile, context);
            }
        }

        /// <summary>
        ///  生成したソースコードを保存する時のファイル名を取得します。
        /// </summary>
        string GetGeneratedCodeFileName(MFCFriendly setting, int number) {
            return setting.NameSpace + "." + generatedBaseFilename + "_" + number.ToString();
        }

        void ProcessSettingFile(AdditionalText xmlFile, GeneratorExecutionContext context) {
            var text = xmlFile.GetText(context.CancellationToken)!.ToString();
            var serializer = new XmlSerializer(typeof(FriendlyDriverGenerator));
            var reader = new StringReader(text);
            var setting = (FriendlyDriverGenerator)serializer.Deserialize(reader);

            var table = new MFCFriendlyDriverTable();

            int number = -1;
            foreach (var mfcFriendly in setting.MFCFriendly) {
                number++;
                var projectDir = Path.Combine(Path.GetDirectoryName(xmlFile.Path), mfcFriendly.ProjectDir);
                var info = new PreprocessProcInfo(projectDir,
                                                  mfcFriendly.Defines.Select(x => x.Value).ToEqList(),
                                                  mfcFriendly.IncludeFile.Select(x => x.IncludePath).ToEqList());
                var (FileTextInProject, PreCompiledString) = Precompile(info, mfcFriendly.RcFilePath);
                // 本当はプロプロセッサの条件分岐をして有効なdefineと無効なdefineを識別するべきであるが、
                // できていないので、
                // 複数個定義されており値が異なる場合は、定義していないものとして扱う
                var definedValues = GetDefineConstValues(string.Join("\n", FileTextInProject))
                    .GroupBy(x => x.Key)
                    .Where(@group => @group.Distinct().Count() <= 1)
                    .SelectMany(xs => xs)
                    .ToDictionary(x => x.Key, x => x.Value);
                var dialogs = table.ToDialogs(
                    ResourceParser.Resources.End()
                    .Parse(PreCompiledString)
                    .OfType<DIALOG>(),
                    definedValues)
                    .ToEqList();

                var template = new FriendlyDriverTemplate(new FriendlyDriverTemplateContext(toolInfo, mfcFriendly.NameSpace, dialogs));
                context.AddSource(GetGeneratedCodeFileName(mfcFriendly, number), template.TransformText());
            }
        }
    }
}
