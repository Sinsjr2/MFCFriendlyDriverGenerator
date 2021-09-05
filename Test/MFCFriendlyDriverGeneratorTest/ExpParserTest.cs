using NUnit.Framework;
using Sprache;

namespace MFCFriendlyDriverGenerator {

    [TestFixture]
    public class ExpParserTest {

        public record StringTestData(string Expected, string Code);

        public static StringTestData[] GetStringTestData() {
            return new StringTestData[] {
                new("", "\"\""),
                new("\\", "\"\\\\\""),
                new("\n", "\"\\n\""),
                new("\t", "\"\\t\""),
                new("\"", "\"\\\"\""),
                new("abzAZ_あ09漢字\" ", "\"abzAZ_あ09漢字\\\" \""),
                new("__a_\"___\"_z__", "  \"__a_\\\"___\\\"_z__\"  "),
            };
        }

        [Test]
        [TestCaseSource(nameof(GetStringTestData))]
        public void StringLiteralTest(StringTestData data) {
            ExpParser.StringLiteral.End().Parse(data.Code).Is(data.Expected);
        }

        [Test]
        [TestCaseSource(nameof(GetStringTestData))]
        public void ExpStringLiteralTest(StringTestData data) {
            ExpParser.Exp.End().Parse(data.Code).Is(new StringLiteral(data.Expected));
        }

        [Test]
        [TestCase("IDD_HOGE", "IDD_HOGE")]
        [TestCase("IDC_FOO", " IDC_FOO \n")]
        [TestCase("_hogehoge", "\r\n_hogehoge \t\n")]
        public void IdentifierTest(string expected, string code) {
            ExpParser.Identifier.End().Parse(code).Is(expected);
        }

        public record ExpTestData(string Code, IExp Expression);
        public static ExpTestData[] GetExpTestData() {
            return new ExpTestData[] {
                new("variable", new Identifier("variable")),
                new("v1 | v2", new BinOperator("|", new Identifier("v1"), new Identifier("v2"))),
                new("20 + 50", new BinOperator("+", new IntegerLiteral(20), new IntegerLiteral(50))),
                new("NOT ENABLE", new UnaryOperator("NOT", new Identifier("ENABLE"))),
                new("! ENABLE", new UnaryOperator("!", new Identifier("ENABLE"))),
                new("-999", new UnaryOperator("-", new IntegerLiteral(999))),
                new("100", new IntegerLiteral(100)),
                new("0xFF", new IntegerLiteral(0xFF)),
                new("0xaa", new IntegerLiteral(0xaa)),
                new("\"strstr\"", new StringLiteral("strstr")),
            };
        }

        [Test]
        [TestCaseSource(nameof(GetExpTestData))]
        public void ExpTest(ExpTestData data) {
            ExpParser.Exp.End().Parse(data.Code).Is(data.Expression);
        }

    }
}
