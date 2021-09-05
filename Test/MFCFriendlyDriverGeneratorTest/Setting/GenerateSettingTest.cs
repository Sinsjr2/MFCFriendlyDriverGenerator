using System.Collections.Generic.Immutable;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;

namespace MFCFriendlyDriverGenerator.Setting {

    [TestFixture]
    public class GenerateSettingTest {

        public record XMLParseDeserializeTestData(string XmlCode, FriendlyDriverGenerator Expected);
        public static XMLParseDeserializeTestData[] GetXMLParseDeserializeTestData() {
            return new XMLParseDeserializeTestData[] {
                new(
@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<FriendlyDriverGenerator>
  <MFCFriendly nameSpace=""AAA"" projectDir=""PPP"" rcFilePath=""BBB"">
    <Include includePath=""C/c"" />
    <Include includePath=""D/d"" />
    <Define value=""HOGE=123""/>
  </MFCFriendly>
</FriendlyDriverGenerator>
",
new FriendlyDriverGenerator(
    new MFCFriendly[] {
        new("PPP",
            "AAA",
            "BBB",
            new Include[] { new("C/c"), new("D/d") }.ToEqList(),
            new Define[] { new("HOGE=123")
            }.ToEqList())
    }
    .ToEqList())),
                new(
@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<FriendlyDriverGenerator>
  <MFCFriendly nameSpace=""AAA"" projectDir=""PPP"" rcFilePath=""BBB"">
  </MFCFriendly>
</FriendlyDriverGenerator>
",
new FriendlyDriverGenerator(
    new MFCFriendly[] {
        new("PPP",
            "AAA",
            "BBB",
            EqList<Include>.Empty,
            EqList<Define>.Empty)
    }
    .ToEqList()))
            };
        }

        [Test]
        [TestCaseSource(nameof(GetXMLParseDeserializeTestData))]
        public void XMLParseDeserializeTest(XMLParseDeserializeTestData data) {
            var serializer = new XmlSerializer(typeof(FriendlyDriverGenerator));
            var writer = new StringWriter();
            serializer.Serialize(writer, data.Expected);
            var reader = new StringReader(data.XmlCode);
            var obj = (FriendlyDriverGenerator)serializer.Deserialize(reader);
            obj.Is(data.Expected);
        }
    }
}
