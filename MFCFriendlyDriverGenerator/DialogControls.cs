using System.Collections.Generic;
using Sprache;

namespace MFCFriendlyDriverGenerator {

    public enum ControlKind {
        AUTO3STATE,
        AUTORADIOBUTTON,
        CHECKBOX,
        CTEXT,
        DEFPUSHBUTTON,
        EDITTEXT,
        GROUPBOX,
        ICON,
        LISTBOX,
        LTEXT,
        PUSHBOX,
        PUSHBUTTON,
        RADIOBUTTON,
        RTEXT,
        SCROLLBAR,
        STATE3,
        AUTOCHECKBOX,
        COMBOBOX,
    }

    public interface IControlID {
        /// <summary>
        ///  コントロールを特定するための名前
        /// </summary>
        string ID { get; }
    };

    public record ControlID(ControlKind Kind, string ID) : IControlID;
    ///  コントロール名の例: SysListView32
    public record Control(string ControlName, string ID) : IControlID;

    public static class DialogControls {

        static Parser<ControlID> Control(bool requireText, ControlKind identifierName)
        {
            return
                from _ in Parse.String(identifierName.ToString()).Elem()
                from label in requireText
                      ? ExpParser.Exp.Then(label => CommonSyntax.CommaSeparator.Select(_ => label))
                      : Parse.Return(default(IExp))
                from id in ExpParser.Identifier
                from _2 in (from __1 in CommonSyntax.CommaSeparator
                            from x in ExpParser.Exp
                            select x).Repeat(4, 6)
                select new ControlID(identifierName, id);
        }

        static readonly Parser<IControlID> CONTROL =
            from _ in Parse.String("CONTROL").Elem()
            from text in ExpParser.StringLiteral
            from id in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Identifier)
            from @class in CommonSyntax.CommaSeparator.Then(_ => ExpParser.StringLiteral)
            from style in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp)
            from _5 in (from __1 in CommonSyntax.CommaSeparator
                        from __2 in ExpParser.Exp
                        select 0).Repeat(4, 6)
            select new Control(@class, id);


        public static readonly Parser<IEnumerable<IControlID>> Controls =
            CONTROL
            .Or(Control(true  ,ControlKind.AUTO3STATE))
            .Or(Control(true  ,ControlKind.AUTORADIOBUTTON))
            .Or(Control(true  ,ControlKind.CHECKBOX))
            .Or(Control(true  ,ControlKind.CTEXT))
            .Or(Control(true  ,ControlKind.DEFPUSHBUTTON))
            .Or(Control(false ,ControlKind.EDITTEXT))
            .Or(Control(true  ,ControlKind.GROUPBOX))
            .Or(Control(true  ,ControlKind.ICON))
            .Or(Control(false ,ControlKind.LISTBOX))
            .Or(Control(true  ,ControlKind.LTEXT))
            .Or(Control(true  ,ControlKind.PUSHBOX))
            .Or(Control(true  ,ControlKind.PUSHBUTTON))
            .Or(Control(true  ,ControlKind.RADIOBUTTON))
            .Or(Control(true  ,ControlKind.RTEXT))
            .Or(Control(false ,ControlKind.SCROLLBAR))
            .Or(Control(true  ,ControlKind.STATE3))
            .Or(Control(true  ,ControlKind.AUTOCHECKBOX))
            .Or(Control(false ,ControlKind.COMBOBOX))
            .Many();
    }

}
