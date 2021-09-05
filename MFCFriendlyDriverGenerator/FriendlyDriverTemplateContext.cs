using System.CodeDom.Compiler;
using System.Collections.Generic.Immutable;

namespace MFCFriendlyDriverGenerator {
    public record FriendlyDriverTemplateContext(GeneratedCodeAttribute ToolInfo, string NameSpace, EqList<Dialog> Dialogs);

    public record Dialog(string DialogName, EqList<ControlInfo> Controls);

    /// <summary>
    ///  MFCのドライバを生成するのに必要なコントロールの情報
    ///  コントロールの名前は変数に名前をつける時に使用
    ///  ドライバの型は名前空間を含んだ型名
    /// </summary>
    public record ControlInfo(int ControlID, string ControlName, string DriverType);
}
