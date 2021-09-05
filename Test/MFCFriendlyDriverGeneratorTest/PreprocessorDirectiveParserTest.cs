using NUnit.Framework;
using Sprache;
using System;
using System.Collections.Generic.Immutable;
using System.IO;
using System.Linq;

namespace MFCFriendlyDriverGenerator {

    [TestFixture]
    public class PreprocessorDirectiveParserTest {

        [Test]
        [TestCase("# pragma hoge hoge\r\n", "")]
        [TestCase("# pragma hoge hoge", "")]
        [TestCase("# pragma hoge hoge\r\n" +
                  "あいうえお\r\n" +
                  "#define DEF\r\n" +
                  "漢字", "あいうえお\r\n" +
                  "漢字")]
        [TestCase("# pragma hoge hoge\r\n" +
                  "12345678\r\n" +
                  "#define hoge 777\r\n", "12345678\r\n")]
        [TestCase("#define foo \\\r\n" +
                  "123456 \\\r\n" +
                  "abcde\r\n" +
                  "あいうえお", "あいうえお")]
        public void DirectiveRegexReplaceTest(string str, string expected) {
            PreprocessorDirectiveParser.DirectiveRegex.Replace(str, "")
                .Is(expected);
        }

        public record PreprocessorDirectiveTestData(string Code, IPreprocessorDirective[] Directives);

        public static PreprocessorDirectiveTestData[] GetPreprocessorDirectiveTestData() {
            return new PreprocessorDirectiveTestData[] {
                new("  #include <stdio>\r\n", new[] { new Include("<stdio>") }),
                new("#include \"hoge.h\"\r\n", new[] { new Include("\"hoge.h\"") }),
                new("  #pragma pack(1)\r\n", new[] { new OtherDirective("pragma pack(1)") }),
                new("  #pragma pack(1) //this is comment\r\nABCDE", new IPreprocessorDirective[] { new OtherDirective("pragma pack(1) "), new TextBlock("ABCDE") }),
                new("#define IDC_BTN 605\r\n", new[] { new Define("IDC_BTN", "605") }),
                new("#define IDM_ABOUTBOX                    0x0010", new[] { new Define("IDM_ABOUTBOX", "0x0010") }),
                new(" #define IDC_A 605\r\n#define IDD_68AA", new[] { new Define("IDC_A", "605"), new Define("IDD_68AA", "") }),
                new("#define IDC_BTN \\\r\n 605 \\\n500\r\n", new[] { new Define("IDC_BTN", " 605 500") }),
                new("#define IDC_BTN 0\r\nhogehoge\r\n#define ZZZ", new IPreprocessorDirective[] { new Define("IDC_BTN", "0"), new TextBlock("hogehoge\r\n"), new Define("ZZZ", "") }),
                new("//hogehoge", Array.Empty<IPreprocessorDirective>()),
                new("#define aaa /*\r\nabce 125\r\n12345678*/qwerty\r\n#include \"io\"", new IPreprocessorDirective[] { new Define("aaa", "qwerty"), new Include("\"io\"") }),
                new("hog/*e\r\n#define ABCDE \r\n#include \"123.h\"\r\n*/12345678", new IPreprocessorDirective[] { new TextBlock("hog 12345678") }),
                new("#pragma pack /* aaa*/ /* bb */ /*ccc*/ ( 4 /* */ ) // #include ", new IPreprocessorDirective[] { new OtherDirective("pragma pack    ( 4  ) ") }),
                new("ABCDE/*\r\n12345678*/abcde", new[] { new TextBlock("ABCDE abcde") } ),
                new("//{{NO_DEPENDENCIES}}, \r\n", Array.Empty<IPreprocessorDirective>()),
                new("//{{NO_DEPENDENCIES}}\r\n" +
                    "// Microsoft Visual C++ で生成されたインクルード ファイル。\r\n" +
                    "// NativeControls.rc で使用\r\n" +
                    "//\r\n" +
                    "#define IDM_ABOUTBOX                    0x0010\r\n" +
                    "#define IDD_ABOUTBOX                    100\r\n", new [] { new Define("IDM_ABOUTBOX", "0x0010"), new Define("IDD_ABOUTBOX", "100") }),
                new("\r\n" +
                    "// Microsoft Visual C++ generated resource script.", Array.Empty<IPreprocessorDirective>()),
                new("#define APSTUDIO_READONLY_SYMBOLS\n" +
                    "/////////////////////////////////////////////////////////////////////////////", new[] { new Define("APSTUDIO_READONLY_SYMBOLS", "") })
            };
        }

        [Test]
        [TestCaseSource(nameof(GetPreprocessorDirectiveTestData))]
        public void PreprocessorDirectiveTest(PreprocessorDirectiveTestData data) {
            PreprocessorDirectiveParser.PreprocessorDirective
                .End()
                .Parse(data.Code).ToEqList()
                .Is(data.Directives.ToEqList());
        }

        public record コメント以外と分離できるかData(string Code, (string kind, string str)[] Expected);

        public static コメント以外と分離できるかData[] Getコメント以外と分離できるかData() {
            return new コメント以外と分離できるかData[] {
                new("hoge/*  ksldkj*/foo", new[] { ("other", "hoge"), ("comment", "  ksldkj"), ("other", "foo") }),
                new("//hoge  ksl", new[] { ("comment", "hoge  ksl") }),
                new("", Array.Empty<(string, string)>()),
                new("A", new[] { ("other", "A") })

            };
        }

        [Test]
        [TestCaseSource(nameof(Getコメント以外と分離できるかData))]
       public void コメント以外と分離できるか(コメント以外と分離できるかData data) {
            var comment = CommonSyntax.Comment.AnyComment;
            var parser = comment.Select(com => ("comment", com))
                .Or(comment.Other().Select(other => ("other", other))).XMany();

            var result = parser.End().Parse(data.Code).ToArray();
            result.Is(data.Expected);
        }

        [Test]
        public void サンプル() {
            var str = File.ReadAllText(@"C:\Users\making-win\Desktop\MFCFriendlyDriverGenerator\Test\DriverGenerateTest\Generated\join.txt");
            var result = PreprocessorDirectiveParser.PreprocessorDirective.End().Parse(str);
        }
    }
}
