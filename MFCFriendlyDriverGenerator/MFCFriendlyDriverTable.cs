using System.Collections.Generic;
using System.Collections.Generic.Immutable;
using System.Linq;

namespace MFCFriendlyDriverGenerator {

    // /// <summary>
    // ///  ドライバに変換した結果
    // ///  リソース名からIDを引けなかった場合にリストに追加して返します。
    // ///  それに失敗しなければ空を返します。
    // /// </summary>
    // public record ConvertResult(EqList<string> MissingResourceName, EqList<Dialog> Dialogs);

    /// <summary>
    ///  RCファイルに記述されたコントロール名からUIのドライバーの型に変換するためのテーブル
    /// </summary>
    public class MFCFriendlyDriverTable {

        readonly IReadOnlyDictionary<ControlKind, string> ControlKindToDriverType = new Dictionary<ControlKind, string>() {
            {ControlKind.AUTO3STATE,      "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.AUTORADIOBUTTON, "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.CHECKBOX,        "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.CTEXT,           "Codeer.Friendly.Windows.Grasp.WindowControl"},
            {ControlKind.DEFPUSHBUTTON,   "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.EDITTEXT,        "Codeer.Friendly.Windows.NativeStandardControls.NativeEdit"},
            // 未割り当て
            // {ControlKind.GROUPBOX,        "Codeer.Friendly.Windows.NativeStandardControls."},
            // {ControlKind.ICON,            "Codeer.Friendly.Windows.NativeStandardControls."},
            {ControlKind.LISTBOX,         "Codeer.Friendly.Windows.NativeStandardControls.NativeListBox"},
            {ControlKind.LTEXT,           "Codeer.Friendly.Windows.Grasp.WindowControl"},
            {ControlKind.PUSHBOX,         "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.PUSHBUTTON,      "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.RADIOBUTTON,     "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.RTEXT,           "Codeer.Friendly.Windows.Grasp.WindowControl"},
            {ControlKind.SCROLLBAR,       "Codeer.Friendly.Windows.NativeStandardControls.NativeScrollBar"},
            {ControlKind.STATE3,          "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.AUTOCHECKBOX,    "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {ControlKind.COMBOBOX,        "Codeer.Friendly.Windows.NativeStandardControls.NativeComboBox"},
        };

        readonly IReadOnlyDictionary<string, string> ControlNameToDriverType = new Dictionary<string, string>() {
            {"Button"            , "Codeer.Friendly.Windows.NativeStandardControls.NativeButton"},
            {"ComboBoxEx32"      , "Codeer.Friendly.Windows.NativeStandardControls.NativeComboBox"},
            {"SysDateTimePick32" , "Codeer.Friendly.Windows.NativeStandardControls.NativeDateTimePicker"},
            {"SysIPAddress32"    , "Codeer.Friendly.Windows.NativeStandardControls.NativeIPAddress"},
            {"SysTabControl32"   , "Codeer.Friendly.Windows.NativeStandardControls.NativeTab"},
            {"msctls_updown32"   , "Codeer.Friendly.Windows.NativeStandardControls.NativeSpinButton"},
            {"msctls_trackbar32" , "Codeer.Friendly.Windows.NativeStandardControls.NativeSlider"},
            {"SysMonthCal32"     , "Codeer.Friendly.Windows.NativeStandardControls.NativeMonthCalendar"},
            {"msctls_progress32" , "Codeer.Friendly.Windows.NativeStandardControls.NativeProgress"},
            {"RichEdit20W"       , "Codeer.Friendly.Windows.NativeStandardControls.NativeEdit"},
            {"RichEdit20A"       , "Codeer.Friendly.Windows.NativeStandardControls.NativeEdit"},
            {"SysListView32"     , "Codeer.Friendly.Windows.NativeStandardControls.NativeListControl"},
            {"SysTreeView32"     , "Codeer.Friendly.Windows.NativeStandardControls.NativeTree"}
        };

        public IEnumerable<Dialog> ToDialogs(IEnumerable<DIALOG> dialogs, IReadOnlyDictionary<string, int> resourceNameToDialogID) {
            return
                from dlg in dialogs
                select new Dialog(dlg.ID,
                                  (from control in dlg.Controls
                                   let maybeResID = resourceNameToDialogID.TryGetValue(control.ID, out var id) ? id : default(int?)
                                   where maybeResID.HasValue
                                   let maybeDriverType = control switch {
                                      Control ctrl => ControlNameToDriverType.TryGetValue(ctrl.ControlName, out var ctrlType) ? ctrlType : null,
                                      ControlID ctrlID => ControlKindToDriverType.TryGetValue(ctrlID.Kind, out var ctrlType) ? ctrlType : null,
                                      _ => null
                                   }
                                   where maybeResID is not null
                                   select new ControlInfo(maybeResID.Value, control.ID, maybeDriverType))
                                  .ToEqList());
        }
    }
}
