using System;
using System.Collections.Generic;
using System.Collections.Generic.Immutable;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sprache;

namespace MFCFriendlyDriverGenerator {

    [TestFixture]
    public class MFCFriendlyDriverGeneratorTest {
        string ProjectRoot => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", ".."));
        string TestDataDir => Path.Combine(ProjectRoot, "resource");

        IRcFilePreCompile PreCompile => new CLPreprocess();
        MFCFriendlyDriverGenerator Generator => new(PreCompile);

        /// <summary>
        ///  テスト用のRCファイルを読み込むための設定
        /// </summary>
        PreprocessProcInfo TestDataInfo => new(TestDataDir, Array.Empty<string>(), Array.Empty<string>());

        /// <summary>
        ///\nを 実行しているOSでの改行コードに変換します
        /// </summary>
        /// <returns></returns>
        static string ToOSNewLine(string str) {
            return str.Replace("\n", Environment.NewLine);
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void プリプロセッサで期待するコメントアウトしたコードが含まれるか() {
            var rcFilepath = "contain_comment.rc";
            FileLoadTest(TestDataDir, Array.Empty<string>(), Array.Empty<string>(), rcFilepath,
                         new( Array.Empty<string>().ToEqList(),
                              "\n// comment\n" +
                              "// comment2\n" +
                              "// #define comment\n\n\n\n" +
                              "end\n"));
        }

        [Test]
        public void インクルードを使用している時そのファイル名を取得できるか() {
            var rcFilepath = "include.rc";
            FileLoadTest(TestDataDir, Array.Empty<string>(), Array.Empty<string>(), rcFilepath,
                         new( new[] { Path.Combine(TestDataDir, "contain_comment.rc"), Path.Combine(TestDataDir, "empty.rc") }.ToEqList(),
                              "\n\n// comment\n" +
                              "// comment2\n" +
                              "// #define comment\n\n\n\n" +
                              "end\n\n" +
                              "including contain_comment file\n\n\n"));
        }

        void FileLoadTest(string workingDir, IReadOnlyList<string> defines, IReadOnlyList<string> includePaths, string rcFilePath, PreprocessResult expected) {
            var result = PreCompile.Preprocess(TestDataInfo, rcFilePath);
            result.PrecompiledCode.Is(ToOSNewLine(expected.PrecompiledCode));
            result.RefFilePaths.Count.Is(expected.RefFilePaths.Count);
            foreach (var (a, b) in result.RefFilePaths.Zip(expected.RefFilePaths, (a, b) =>(a, b))) {
                Assert.That(a, Is.SamePath(b).IgnoreCase);
            }
        }

        [Test]
        public void 存在しないディレクトリを作業ディレクトリにすると例外が発生するか() {
            var directoryName = Path.Combine(TestDataDir, "hogeDir");
            Assert.That(() =>
                        PreCompile.Preprocess(TestDataInfo with { ProjectDir = directoryName }, "contain_comment.rc"),
                        Throws.Exception.TypeOf<DirectoryNotFoundException>()
                        .And.Message.EqualTo(directoryName));
        }

        readonly string DialogTestRCCode =
            "IDD_OPEN DIALOGEX 0, 0, 286, 78\n" +
            "STYLE DS_SETFONT | DS_MODALFRAME | DS_FIXEDSYS | WS_POPUP | WS_CAPTION | WS_SYSMENU\n" +
            "CAPTION \"Load Images\"\n" +
            "FONT 9, \"Segoe UI\", 400, 0, 0x1\n" +
            "BEGIN\n" +
            "    LTEXT           \"Left image\",IDC_STATIC,7,9,59,8\n" +
            "    EDITTEXT        IDC_LEFTIMAGE,66,7,187,14,ES_AUTOHSCROLL\n" +
            "    PUSHBUTTON      \"...\",IDC_LEFTBROWSE,258,7,21,14\n" +
            "    LTEXT           \"Right image\",IDC_STATIC,7,33,59,8\n" +
            "    EDITTEXT        IDC_RIGHTIMAGE,66,31,187,14,ES_AUTOHSCROLL\n" +
            "    PUSHBUTTON      \"...\",IDC_RIGHTBROWSE,258,31,21,14\n" +
            "    DEFPUSHBUTTON   \"OK\",IDOK,169,57,50,14\n" +
            "    PUSHBUTTON      \"Cancel\",IDCANCEL,229,57,50,14\n" +
            "END\n";

        [Test]
        public void 本文のDefineを展開せずにファイルをプリコンパイルできるか() {
            Generator.PrecompiledString(TestDataInfo, "DialogTest.rc")
                .Is(ToOSNewLine(DialogTestRCCode));
        }

        [Test]
        public void Defineを展開せずにファイルをプリコンパイルできるか_相対パスでインクルードした場合() {
            Generator.PrecompiledString(new(TestDataDir, Array.Empty<string>(), Array.Empty<string>()), "sub_dir\\include_DialogTest.rc")
                .Is(ToOSNewLine("including DialogTest.rc\n" +
                    DialogTestRCCode));
        }

        [Test]
        public void MFCのRCファイルをパースできるか() {
            var env = Environment.GetEnvironmentVariable("INCLUDE");
            var workingDir = Path.Combine(Path.GetFullPath(Path.Combine(ProjectRoot, "..")), "Target", "NativeControls");
            var rcfile = "NativeControls.rc";
            var rcBody = Generator.PrecompiledString(new(workingDir, Array.Empty<string>(), Array.Empty<string>()), rcfile);
            var resources = ResourceParser.Resources.End().Parse(rcBody).ToArray();
            Assert.That(resources, Is.Not.Empty);
        }
    }
}
