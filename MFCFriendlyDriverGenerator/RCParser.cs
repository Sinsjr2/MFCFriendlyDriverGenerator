using System.Collections.Generic;
using System.Collections.Generic.Immutable;
using System.Linq;
using Sprache;

namespace MFCFriendlyDriverGenerator {

    public interface IResource { }

    public enum FileResourceKind {
        BITMAP,
        CURSOR,
        FONT,
        HTML,
        ICON,
        MESSAGETABLE,
        CONFIG,
    }

    public record FileResource(FileResourceKind Kind, string ID, string Filename) : IResource;
    public record DIALOG(string ID, EqList<IControlID> Controls, string? MenuID) : IResource;
    public record DIALOGEXOptional(IExp Style, string Caption);
    public record Accelerator(IExp Event, string IdValue);
    public record Accelerators(string ID, EqList<Accelerator> Elements) : IResource;
    public record StringResouce(string ID, string Str) : IResource;
    public record Toolbar(string ID, EqList<string> ButtonIDs) : IResource;
    public record TextInclude(IExp ID, EqList<string> Codes) : IResource;
    public record Language(IExp Lang, IExp SubLang) : IResource;

    public record Menu(string ID, EqList<IMenuItem> Items) : IResource;
    public record DlgInit(string ID) : IResource;
    public interface IMenuItem { }
    public record Popup(string Text, EqList<IMenuItem> Items) : IMenuItem;
    public record MenuItem(IExp ID, string Text) : IMenuItem;
    public record MenuSeparator() : IMenuItem;
    public record VersionInfo(IExp ID) : IResource;
    public record AFXDialogLayout(string ID, EqList<int> Values) : IResource;

    public static class ResourceParser {


        static readonly Parser<IResource> DIALOGEX = DialogBase("DIALOGEX", 3, 4);
        static readonly Parser<IResource> DIALOG   = DialogBase("DIALOG", 3, 3);

        static readonly Parser<string> DialogOptional =
            Parse.String("CAPTION").Elem().Then(_ => ExpParser.StringLiteral.Select(_ => "")).Or(
                Parse.String("STYLE").Elem().Then(_ => ExpParser.Exp.Select(_ => ""))
            )
            .Or(Parse.String("EXSTYLE").Elem().Then(_ => ExpParser.Exp.Select(_ => "")))
            .Or(Parse.String("MENU").Elem().Then(_ => ExpParser.Identifier))
            .Or(
                from __1 in Parse.String("FONT").Elem()
                from pointSize in ExpParser.Exp
                from typeface in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp)
                from weight_italic_charset in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp).Repeat(0, 3)
                select ""
            ).Or(
                from _1 in Parse.String("CLASS").Elem()
                from @class in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp)
                select ""
            ).Or(Parse.Ref(() => LANGUAGE).Select(_ => ""))
            .Many()
            .Select(xs => xs.Where(str => str != "").SingleOrDefault());

        static Parser<IResource> DialogBase(string identifierName, int? min, int? max)
        {
            return
                from nameID in ExpParser.Identifier
                from _1 in Parse.String(identifierName).Elem()
                from x in ExpParser.Exp
                from _2 in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp).Repeat(min, max)
                from menuID in DialogOptional
                from controls in DialogControls.Controls.BeginEnd()
                select new DIALOG(nameID, controls.ToEqList(), menuID);
        }

        static Parser<IResource> FileResource(FileResourceKind kind)
        {
            var kindToken = Parse.String(kind.ToString()).Elem();
            return
                from nameID in ExpParser.Identifier.Then(id => kindToken.Return(id))
                from filename in ExpParser.StringLiteral
                select new FileResource(kind, nameID, filename);
        }

        static readonly Parser<IResource> ACCELERATORS =
            from acctableName in ExpParser.Identifier
            from _ in Parse.String("ACCELERATORS").Elem()
            from body in (from @event in ExpParser.Exp
                          from idValue in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Identifier)
                          from _2 in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp).Many()
                          select new Accelerator(@event, idValue)).Many().BeginEnd()
        select new Accelerators(acctableName, body.ToEqList());

        static readonly Parser<IEnumerable<IResource>> STRINGTABLE =
            from _1 in Parse.String("STRINGTABLE").Elem()
            from resource in (from stringID in ExpParser.Identifier
                              from str in ExpParser.StringLiteral
                              select new StringResouce(stringID, str)).Many().BeginEnd()
            select resource;

        static readonly Parser<IResource> LANGUAGE =
            from _ in Parse.String("LANGUAGE").Elem()
            from lang in ExpParser.Exp
            from subLang in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp)
            select new Language(lang, subLang);

        static readonly Parser<IMenuItem> POPUP =
            from text in Parse.String("POPUP").Elem().Then(_ => ExpParser.StringLiteral)
            from menuItems in MENUITEM.Or(POPUP).Many().BeginEnd()
            select new Popup(text, menuItems.ToEqList());
        static readonly Parser<IMenuItem> MENUITEM =
            from _ in Parse.String("MENUITEM").Elem()
            from item in Parse.String("SEPARATOR").Elem().Select(_ => new MenuSeparator()).Or<IMenuItem>(
                from text in ExpParser.StringLiteral
                from id in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp)
                from __ in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp).Repeat(0, 1)
                select new MenuItem(id, text)
            )
            select item;
        static readonly Parser<IResource> MENU =
            from id in ExpParser.Identifier
            from _ in Parse.String("MENU").Elem()
            from menuItems in POPUP.Or(MENUITEM).XMany().BeginEnd()
            select new Menu(id, menuItems.ToEqList());

        static readonly Parser<IResource> DLGINIT =
            from id in ExpParser.Identifier
            from _1 in Parse.String("DLGINIT").Elem()
            from _2 in (
                from rows in (
                    from innnerId in ExpParser.Identifier
                    from ___1 in Parse.String(",").Elem()
                    from ___2 in Parse.String("0x403").Elem()
                    from ___3 in Parse.String(",").Elem()
                    from strLength in ExpParser.IntLiteral
                    from ___4 in Parse.String(",").Elem()
                    from ___5 in Parse.String("0").Elem()
                    from str  in ExpParser.HexLiteral.Return("").Or(
                        ExpParser.StringLiteral).DelimitedBy(Parse.String(",").Elem())
                    .Many()
                    from ___6 in Parse.String(",").Elem().Optional()
                    select "")
                .Many()
                from __1 in Parse.String("0").Elem()
                select ""
            ).XOptional().BeginEnd()
            select new DlgInit(id);

        static readonly Parser<IResource> TOOLBAR =
            from toolbarID in ExpParser.Identifier
            from _1 in Parse.String("TOOLBAR").Elem().Then(_ => ExpParser.Exp)
            from _2 in CommonSyntax.CommaSeparator.Then(_ => ExpParser.Exp)
            from btns in Parse.String("BUTTON").Elem().Then(_ => ExpParser.Identifier)
                         .Or(Parse.String("SEPARATOR").Elem().Select(_ => default(string)))
                         .XMany()
                         .BeginEnd()
            select new Toolbar(toolbarID, btns.Where(maybeBtn => maybeBtn is not null).ToEqList());

        static readonly Parser<IResource> TEXTINCLUDE =
            from id in ExpParser.IntLiteral
            from codes in Parse.String("TEXTINCLUDE").Elem()
                          .Then(_ => ExpParser.StringLiteral.XMany().BeginEnd())
            select new TextInclude(new IntegerLiteral(id), codes.ToEqList());

        static readonly Parser<IEnumerable<char>> BLOCK = Parse.String("BLOCK").Elem();
        static readonly Parser<IEnumerable<char>> VALUE = Parse.String("VALUE").Elem();

        static readonly Parser<string> StringFileInfoBLOCK =
            from _ in BLOCK.Then(_ => Parse.String("\"StringFileInfo\"").Elem())
            from _2 in CommonSyntax.BeginEnd(
                (from langCharset in BLOCK.Then(_ => ExpParser.Exp)
                 from __1 in CommonSyntax.BeginEnd(
                     VALUE
                     .Then(_ => ExpParser.Exp)
                     .Then(_ => CommonSyntax.CommaSeparator)
                     .Then(_ => ExpParser.Exp)
                     .Many())
                 select "")
                .XMany()
            )
            select "";

        static readonly Parser<string> VarFileInfoBLOCK =
            from _ in BLOCK.Then(_ => Parse.String("\"VarFileInfo\"").Elem())
            from _2 in CommonSyntax.BeginEnd(
                VALUE
                .Then(_ => ExpParser.Exp)
                .Then(_ => CommonSyntax.CommaSeparator)
                .Then(_ => ExpParser.Exp)
                .Then(_ => CommonSyntax.CommaSeparator)
                .Then(_ => ExpParser.Exp)
                .XMany()
            )
            select "";

        static readonly Parser<IResource> VERSIONINFO =
            // from id in ExpParser.Exp
            from id in ExpParser.Identifier
            let integers = ExpParser.Exp.DelimitedBy(CommonSyntax.CommaSeparator)
            .Select(_ => default(IExp))
            from _1 in Parse.String("VERSIONINFO").Elem()
            from _2 in Parse.String("FILEVERSION").Elem().Then(_ => integers)
            .Or(Parse.String("PRODUCTVERSION").Elem().Then(_ => integers))
            .Or(Parse.String("FILEFLAGSMASK").Elem().Then(_ => ExpParser.Exp))
            .Or(Parse.String("FILEFLAGS").Elem().Then(_ => ExpParser.Exp))
            .Or(Parse.String("FILEOS").Elem().Then(_ => ExpParser.Exp))
            .Or(Parse.String("FILETYPE").Elem().Then(_ => ExpParser.Exp))
            .Or(Parse.String("FILESUBTYPE").Elem().Then(_ => ExpParser.Exp))
            .Repeat(7)
            from _3 in VarFileInfoBLOCK.Or(StringFileInfoBLOCK).Many().BeginEnd()
            select new VersionInfo(new Identifier(id));

        static readonly Parser<IResource> AFX_DIALOG_LAYOUT =
            from id in ExpParser.Identifier
            from _ in Parse.String("AFX_DIALOG_LAYOUT").Elem()
            from values in ExpParser.IntLiteral.Elem()
                .DelimitedBy(Parse.Char(',').Elem())
                .BeginEnd()
            select new AFXDialogLayout(id, values.ToEqList());

        public static readonly Parser<IEnumerable<IResource>> Resources =
            DIALOGEX
            .Or(DIALOG)
            .Or(DLGINIT)
            .Or(FileResource(FileResourceKind.BITMAP))
            .Or(FileResource(FileResourceKind.CURSOR))
            .Or(FileResource(FileResourceKind.FONT))
            .Or(FileResource(FileResourceKind.HTML))
            .Or(FileResource(FileResourceKind.ICON))
            .Or(FileResource(FileResourceKind.MESSAGETABLE))
            .Or(FileResource(FileResourceKind.CONFIG))
            .Or(ACCELERATORS)
            .Or(TOOLBAR)
            .Or(TEXTINCLUDE)
            .Or(LANGUAGE)
            .Or(MENU)
            .Or(VERSIONINFO)
            .Or(AFX_DIALOG_LAYOUT)
            .Select(res => new[] { res }.AsEnumerable())
            .Or(STRINGTABLE)
            .XMany()
            .Elem()
            .Select(xs => xs.SelectMany(x => x));
    }
}
