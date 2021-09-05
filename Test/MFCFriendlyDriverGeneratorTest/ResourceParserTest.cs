using System;
using System.Collections.Generic.Immutable;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sprache;

namespace MFCFriendlyDriverGenerator {

    [TestFixture]
    public class ResourceParserTest {

        [Test]
        [TestCase(FileResourceKind.ICON, "IDI_HOGE", @"..\hoge.ico", "IDI_HOGE        ICON                    \"..\\\\hoge.ico\"\r\n")]
        [TestCase(FileResourceKind.BITMAP, "IDB_FOO", @"..\hoge.bmp", "IDB_FOO        BITMAP                    \"..\\\\hoge.bmp\"\r\n")]
        [TestCase(FileResourceKind.CURSOR, "IDC_CUR", @"..\Resources\hoge.cur", "IDC_CUR              CURSOR                  \"..\\\\Resources\\\\hoge.cur\"\r\n")]
        [TestCase(FileResourceKind.CONFIG, "IDR_INI", @"Regex.ini", "IDR_INI CONFIG  \"Regex.ini\"")]
        public void FileResourceTest(FileResourceKind expectedKind, string expectedID, string expectedFilename, string code) {
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] { new FileResource(expectedKind, expectedID, expectedFilename) });
        }

        [Test]
        [TestCase("")]
        [TestCase("STYLE DS_SETFONT | DS_MODALFRAME")]
        [TestCase("CAPTION \"this is caption\"")]
        [TestCase("FONT 9, \"Segoe UI\", 400, 0, 0x1")]
        [TestCase("STYLE DS_SETFONT | DS_MODALFRAME\r\n" +
                  "CAPTION \"this is caption\"\r\n" +
                  "FONT 9, \"Segoe UI\", 400, 0, 0x1")]
        [TestCase("EXSTYLE WS_EX_APPWINDOW\r\n")]
        [TestCase("MENU IDR_MAIN_MENU\r\n", "IDR_MAIN_MENU")]
        public void DialogOptionTest(string options, string? menuID = null) {
            var id = "IDD";
            var code = id + " DIALOGEX 0, 0, 20, 10\r\n" +
                options +
                " BEGIN END";
            ResourceParser.Resources.End().Parse(code).Is(new[] { new DIALOG(id, EqList<IControlID>.Empty, menuID) });
        }


        string CreateDialogBase(string id, string dialog, string controls) {
            return $"{id} {dialog} 0, 0, 286, 78\n" +
                "STYLE DS_SETFONT | DS_MODALFRAME | DS_FIXEDSYS | WS_POPUP | WS_CAPTION | WS_SYSMENU\n" +
                "CAPTION \"Images\"\n" +
                "FONT 9, \"Segoe UI\", 400, 0, 0x1\n" +
                "BEGIN\n" +
                controls +
                " END";
        }

        public record DialogTestControlData(string Code, EqList<IControlID> Controls);
        public static DialogTestControlData[] GetDialogControlTestData() {
            static DialogTestControlData Row(string code, (ControlKind controlKind, string id)[] controls) {
                return new(code, controls.Select(ctrl => (IControlID)new ControlID(ctrl.controlKind, ctrl.id)).ToEqList());
            }

            return new[] {
                Row("LTEXT           \"image\",IDC_STATIC,0,0,100,10", new[] { (ControlKind.LTEXT, "IDC_STATIC") }),
                Row("EDITTEXT        IDC_EDIT,66,7,187,14,ES_AUTOHSCROLL", new[] { (ControlKind.EDITTEXT, "IDC_EDIT") }),
                Row("PUSHBUTTON      \"...\",IDC_BROWSE,200,9,99,10", new[] { (ControlKind.PUSHBUTTON, "IDC_BROWSE") }),
                Row("DEFPUSHBUTTON   \"OK\",IDOK,169,57,50,14", new[] { (ControlKind.DEFPUSHBUTTON, "IDOK") }),
                Row(" ICON            IDI_icon , IDC_STATIC , 15 , 15 , 21 , 20", new[] { (ControlKind.ICON, "IDC_STATIC") }),
                Row("    COMBOBOX        IDC_COMBO,85,49,136,30,CBS_DROPDOWNLIST | CBS_AUTOHSCROLL | WS_VSCROLL | WS_TABSTOP", new[] { (ControlKind.COMBOBOX, "IDC_COMBO") }),
                Row("GROUPBOX   \"Hoge\",IDC_GROUPBOX,7,123,319,99", new[] { (ControlKind.GROUPBOX, "IDC_GROUPBOX") }),
                Row("    CHECKBOX \"   \",IDC_CHECK,14,36,195,18,BS_VCENTER | BS_MULTILINE", new[] { (ControlKind.CHECKBOX, "IDC_CHECK") }),
                Row("CTEXT           \"...\",IDC_TEXT,7,57,486,8\n,SS_CENTERIMAGE | NOT WS_VISIBLE", new[] { (ControlKind.CTEXT, "IDC_TEXT") }),
                Row("LISTBOX         IDC_LIST,7,23,79,187,LBS_SORT | LBS_NOINTEGRALHEIGHT | WS_VSCROLL | WS_TABSTOP", new[] { (ControlKind.LISTBOX, "IDC_LIST") }),
                Row("RTEXT           \">>\",IDC_A_B,0,0,0,0,NOT WS_VISIBLE", new[] { (ControlKind.RTEXT, "IDC_A_B") }),

                // // 複数個組み合わせる
                Row("LISTBOX IDC_LIST,0,0,0,187,WS_TABSTOP " +
                    "RTEXT \">>\",IDC_A_B,0,0,0,0,WS_VISIBLE", new[] { (ControlKind.LISTBOX, "IDC_LIST"), (ControlKind.RTEXT, "IDC_A_B") }),
                Row("", Array.Empty<(ControlKind, string)>())
            };
        }
        [Test]
        [TestCaseSource(nameof(GetDialogControlTestData))]
        public void DialogControlTest(DialogTestControlData data) {
            string dialogID = "IDD_HOGE";
            ResourceParser.Resources.End().Parse(CreateDialogBase(dialogID, "DIALOGEX", data.Code))
                .Is(new[] { new DIALOG(dialogID, data.Controls, null)});
        }

        public static DialogTestControlData[] GetDialogCONTROLTestData() {
            static DialogTestControlData Row(string code, params (string controlName, string id)[] controls) {
                return new(code, controls.Select(ctrl => (IControlID)new Control(ctrl.controlName, ctrl.id)).ToEqList());
            }

            return new[] {
                Row("  CONTROL  \".\",IDC_BTN,\n \"Button\",BS_AUTOCHECKBOX | WS_DISABLED | WS_TABSTOP,14,129,234,10", ("Button", "IDC_BTN")),
                Row("CONTROL \"\",IDC_LIST,\"SysListView32\",LVS_REPORT | LVS_ALIGNLEFT | LVS_OWNERDATA | WS_BORDER | WS_TABSTOP,7,7,420,113", ("SysListView32", "IDC_LIST")),
                Row("\r\nCONTROL \"\" , IDC_LIST , \"SysListView32\" \r\n,LVS_REPORT\r\n | LVS_ALIGNLEFT | LVS_OWNERDATA | WS_BORDER | WS_TABSTOP,7,7,\r\n420,113\r\n", ("SysListView32", "IDC_LIST")),
            };
        }
        [Test]
        [TestCaseSource(nameof(GetDialogCONTROLTestData))]
        public void DialogCONTROLTest(DialogTestControlData data) {
            string dialogID = "IDD_____";
            ResourceParser.Resources.End()
                .Parse(CreateDialogBase(dialogID + "\r\n", "DIALOGEX", data.Code))
                .Is(new[] { new DIALOG(dialogID, data.Controls, null)});
        }

        [Test]
        [TestCase("__HOGEHOGE\n", "16, 16", "", "__HOGEHOGE")]
        [TestCase("IDR_bar", "1,8", "BUTTON ID_HOGE", "IDR_bar", "ID_HOGE")]
        [TestCase("id_piyo", "10,10", "SEPARATOR", "id_piyo")]
        [TestCase("id", "10,10", "BUTTON ID_BTN1 \r\nSEPARATOR\r\nBUTTON ID_BTN2", "id", "ID_BTN1", "ID_BTN2")]
        public void ToolbarTest(string id, string param, string body, string expectedID, params string[] expectedBtnIDs) {
            var code = id + " TOOLBAR " + param +
                " BEGIN\r\n" +
                body +
                "\nEND";
            ResourceParser.Resources.End().Parse(code).Is(new[] { new Toolbar(expectedID, expectedBtnIDs.ToEqList()) });
        }

        public record StringTableTestData(string Body, EqList<StringResouce> StringResouces);
        public static StringTableTestData[] GetStringTableTestData() {
            static StringTableTestData Row(string body, params (string id, string str)[] stringResouces) {
                return new(body, stringResouces.Select(x => new StringResouce(x.id, x.str)).ToEqList());
            }

            return new[] {
                Row("IDS_STR \"this is string\"", ("IDS_STR", "this is string")),
                Row(""),
                Row("IDS_STR1 \"1\" IDS_STR2 \"2\"", ("IDS_STR1", "1"), ("IDS_STR2", "2")),
            };
        }
        [Test]
        [TestCaseSource(nameof(GetStringTableTestData))]
        public void StringTableTest(StringTableTestData data) {
            var code = "STRINGTABLE\r\n" +
                "BEGIN\r\n" +
                data.Body +
                " END\r\n";
            ResourceParser.Resources.End().Parse(code).Is(data.StringResouces);
        }

        [Test]
        [TestCase(1, "\"../A/resource.h\\0\"\r\n", "../A/resource.h\0")]
        [TestCase(2, "\"A\"\r\n\"B\\0\"", "A", "B\0")]
        public void TextIncludeTest(int id, string body, params string[] expectedCodes) {
            var code = $"{id} TEXTINCLUDE \r\n" +
                "BEGIN\r\n" +
                body +
                " END";
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] {new TextInclude(new IntegerLiteral(id), expectedCodes.ToEqList())});
        }

        [Test]
        [TestCase("LANGUAGE LANG_JAPANESE, SUBLANG_DEFAULT\r\n//\r\n", "LANG_JAPANESE", "SUBLANG_DEFAULT")]
        [TestCase("LANGUAGE LANG_GERMAN, SUBLANG_GERMAN_SWISS", "LANG_GERMAN", "SUBLANG_GERMAN_SWISS")]
        public void LanguageTest(string code, string expectedLang, string expectedSubLang) {
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] { new Language(new Identifier(expectedLang), new Identifier(expectedSubLang)) });
        }

        public record AcceleratorsTestData(string Body, string ID, EqList<Accelerator> Expecteds);
        public static AcceleratorsTestData[] GetAcceleratorsTestData() {
            return new AcceleratorsTestData[] {
                new("\"Q\", ID_EXIT,  VIRTKEY, CONTROL, NOINVERT\r\n", "IDR_HOGE", new[] { new Accelerator(new StringLiteral("Q"), "ID_EXIT") }.ToEqList()),
                new("VK_ESCAPE, ID_A,  VIRTKEY, NOINVERT\r\n", "idr_foo", new[] { new Accelerator(new Identifier("VK_ESCAPE"), "ID_A") }.ToEqList()),
                new("", "IDR", EqList<Accelerator>.Empty),
                // 複数個組み合わせる
                new("\"C\", ID_A\r\n" + "\"W\", ID_B", "IDR_HOGE", new Accelerator[] { new(new StringLiteral("C"), "ID_A"), new(new StringLiteral("W"), "ID_B") }.ToEqList()),
            };
        }

        [Test]
        [TestCaseSource(nameof(GetAcceleratorsTestData))]
        public void AcceleratorsTest(AcceleratorsTestData data) {
            var code = data.ID + " ACCELERATORS\r\n" +
                "BEGIN\r\n" +
                data.Body +
                " END\r\n";
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] {new Accelerators(data.ID, data.Expecteds)});
        }
       public record MenuTestData(string ID,  string Body, EqList<IMenuItem> ExpectedItems);
        public static MenuTestData[] GetMenuTestData() {
            return new MenuTestData[] {
                new("IDR_MENU0", "", EqList<IMenuItem>.Empty),
                new("IDR_MENU1", "POPUP \"&File\" BEGIN\r\n" +
                    "MENUITEM \"A\", ID_1\r\n" +
                    "END", new IMenuItem[] {
                        new Popup("&File",
                                  new IMenuItem[] { new MenuItem(new Identifier("ID_1"), "A") }.ToEqList())
                    }.ToEqList()),
                new("IDR_MENU2", "POPUP \"Edit\" BEGIN\r\n" +
                    "  MENUITEM SEPARATOR\r\n" +
                    "END", new IMenuItem[] {
                        new Popup("Edit",
                                  new IMenuItem[] { new MenuSeparator() }.ToEqList())
                    }.ToEqList()),
                new("IDR_MENU3", @"MENUITEM ""B"", ID_2", new IMenuItem[] { new MenuItem(new Identifier("ID_2"), "B") }.ToEqList()),
                new("IDR_MENU4", "POPUP \"P1\" BEGIN\r\n" +
                    "MENUITEM \"M1\", ID_M1\r\n" +
                    "  POPUP \"P2\" BEGIN\r\n" +
                    "    MENUITEM \"M2\", ID_M2\r\n" +
                    "  END\r\n" +
                    "END", new IMenuItem[] {
                        new Popup("P1",
                                  new IMenuItem[] {
                                      new MenuItem(new Identifier("ID_M1"), "M1"),
                                      new Popup("P2",
                                                new IMenuItem[] {
                                                    new MenuItem(new Identifier("ID_M2"), "M2")
                                                }.ToEqList())
                                  }.ToEqList())
                    }.ToEqList()),

            };
        }
        [Test]
        [TestCaseSource(nameof(GetMenuTestData))]
        public void MenuTest(MenuTestData data) {
            var code = data.ID + " MENU\r\n" +
                "BEGIN\r\n" +
                data.Body +
                "\r\nEND\r\n";
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] { new Menu(data.ID, data.ExpectedItems) });
        }

        [Test]
        [TestCase("VS_VERSION_INFO",
                  "FILEVERSION 8,0,4,0\r\n" +
                  "PRODUCTVERSION 8,0,4,0\r\n" +
                  "FILEFLAGSMASK 0x3fL\r\n" +
                  "FILEFLAGS 0x1L\r\n" +
                  "FILEOS 0x4L\r\n" +
                  "FILETYPE 0x1L\r\n" +
                  "FILESUBTYPE 0x0L\r\n" +
                  "BEGIN\r\n" +
                  "    BLOCK \"StringFileInfo\"\r\n" +
                  "    BEGIN\r\n" +
                  "        BLOCK \"041103a4\"\r\n" +
                  "        BEGIN\r\n" +
                  "            VALUE \"FileVersion\", \"8.0.4.0\"\r\n" +
                  "            VALUE \"comments\", \"this is comment\"\r\n" +
                  "        END\r\n" +
                  "    END\r\n" +
                  "    BLOCK \"VarFileInfo\"\r\n" +
                  "    BEGIN\r\n" +
                  "        VALUE \"Translation\", 0x411, 932\r\n" +
                  "    END\r\n" +
                  "END\r\n")]
            [TestCase("VS_2",
                  "FILESUBTYPE 0\r\n" +
                  "FILETYPE 1\r\n" +
                  "FILEOS 4\r\n" +
                  "FILEFLAGS 0x1\r\n" +
                  "FILEFLAGSMASK 0xFF\r\n" +
                  "PRODUCTVERSION 9,9,9,9\r\n" +
                  "FILEVERSION 9,9,9,9\r\n" +
                  "BEGIN\r\n" +
                  "END\r\n"
                )]
        public void VersionInfoTest(string id, string body) {
            var code = id + " VERSIONINFO\r\n" +
                body;
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] { new VersionInfo(new Identifier(id)) });
        }

        [Test]
        [TestCase("IDD_HOGE", "0x0000", 0)]
        [TestCase("IDD_AZ", "0", 0)]
        [TestCase("IDD_1234", "0x000F", 0xF)]
        [TestCase("IDD_az", "0x000F", 0xF)]
        [TestCase("IDD_az", "0xf,\r\n0, 100, 0, 0", 0xF,  0, 100, 0, 0)]
        public void AFX_DIALOG_LAYOUTTest(string id, string values, params int[] expectedValues) {
            var code =
                id + " AFX_DIALOG_LAYOUT\r\n" +
                "BEGIN\r\n" +
                values +
                " END";
            ResourceParser.Resources.End().Parse(code)
                .Is(new[] { new AFXDialogLayout(id, expectedValues.ToEqList()) });
        }

        [Test]
        [TestCase("STRINGTABLE BEGIN END // comment")]
        [TestCase("/*STRIN=BEGIN END*/")]
        [TestCase("//aaa\r\n//bbb\r\n")]
        [TestCase("//////////\r\n\r\n\r\n///////\r\n\r\n///////\r\n\r\n")]
        [TestCase("      ")]
        public void コメントテスト(string code) {
            ResourceParser.Resources.End().Parse(code);
        }
    }
}
