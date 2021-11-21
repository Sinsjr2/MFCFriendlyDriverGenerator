using System.Collections.Generic.Immutable;
using System.Linq;
using System.Xml.Serialization;

namespace MFCFriendlyDriverGenerator.Setting {

    [XmlRoot("FriendlyDriverGenerator")]
    public record FriendlyDriverGenerator(
        [property:XmlIgnore]
        EqList<MFCFriendly> MFCFriendly
    ) {
        /// <summary>
        /// ソースコードジェネレータのデバッグ用にデバッガをアタッチするかどうかの設定
        /// </summary>
        [XmlAttribute("attachesDebugger")]
        public bool AttachesDebugger { get; init; } = false;

        [XmlElement("MFCFriendly")]
        public MFCFriendly[] MFCFriendlyForXml {
            get => MFCFriendly.ToArray();
            init => MFCFriendly = value.ToEqList();
        }

        public FriendlyDriverGenerator() : this(EqList<MFCFriendly>.Empty) { }
    }

    public record MFCFriendly {

        /// <summary>
        ///  RCファイルから生成するクラスの名前空間
        /// </summary>
        [XmlAttribute("nameSpace")]
        public string NameSpace { get; init; }

        /// <summary>
        ///  読み込むRCファイル
        ///  <see cref="ProjectDir"/> からの相対パスで指定する必要があります。
        /// </summary>
        [XmlAttribute("rcFilePath")]
        public string RcFilePath { get; init; }

        /// <summary>
        ///  ココで指定したディレクトリのサブディレクトリ以下にincludeすべてのRCファイルが
        ///  収まるようにパスを指定する必要があります。
        ///  設定用のxmlファイルをおいているディレクトリからの相対パスで指定する必要があります。
        /// </summary>
        [XmlAttribute("projectDir")]
        public string ProjectDir { get; init; }

        [XmlElement("Include")]
        public Include[] IncludeFilesForXml {
            get => IncludeFile.ToArray();
            init => IncludeFile = value?.ToEqList() ?? EqList<Include>.Empty;
        }

        [XmlIgnore]
        public EqList<Include> IncludeFile { get; init; }

        [XmlElement("Define")]
        public Define[] DefinesForXml {
            get => Defines.ToArray();
            init => Defines = value?.ToEqList() ?? EqList<Define>.Empty;
        }

        [XmlIgnore]
        public EqList<Define> Defines { get; init; }

        public MFCFriendly(string projectDir, string nameSpace, string rcFilePath, EqList<Include> includeFile, EqList<Define> defines) {
            NameSpace = nameSpace;
            RcFilePath = rcFilePath;
            IncludeFile = includeFile;
            Defines = defines;
            ProjectDir = projectDir;
        }

        public MFCFriendly(): this("", "", "", EqList<Include>.Empty, EqList<Define>.Empty) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param cref="IncludePath">RCファイルでincludeするときに読み込むパス (1つだけ)</param>
    public record Include(
        [property:XmlAttribute("includePath")]
        string IncludePath
        ) {

        public Include() : this("") { }
    }

    /// <summary>
    /// </summary>
    /// <param cref="Value">
    ///  RCファイルをプリプロセスする場合に使用するdefine
    ///  HOGE=89 や
    ///  FOO
    ///  といった文字列を指定する
    /// </param>
    public record Define(
        [property:XmlAttribute("value")]
        string Value
        ) {

        public Define() : this("") { }
    }
}
