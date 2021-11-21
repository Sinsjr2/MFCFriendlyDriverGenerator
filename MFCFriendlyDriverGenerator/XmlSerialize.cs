using System.IO;
using System.Xml.Serialization;

namespace MFCFriendlyDriverGenerator {

    public static class XmlSerialize {

        public static T Deserialize<T>(string xmlText) {
            var serializer = new XmlSerializer(typeof(T));
            var reader = new StringReader(xmlText);
            return (T)serializer.Deserialize(reader);
        }

        public static string Serialize<T>(this T obj) {
            var serializer = new XmlSerializer(typeof(T));
            var writer = new StringWriter();
            serializer.Serialize(writer, obj);
            return writer.ToString();
        }
    }
}
