using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sprache;

namespace MFCFriendlyDriverGenerator {
    public interface IPreprocessorDirective { }

    public record Define(string Name, string Value) : IPreprocessorDirective;
    public record Include(string Path) : IPreprocessorDirective;
    public record OtherDirective(string Value) : IPreprocessorDirective;
    public record TextBlock(string Value) : IPreprocessorDirective;

    public static class PreprocessorDirectiveParser {

        static readonly Parser<IEnumerable<char>> EndOfLine =
            Parse.String("\r\n").Or(Parse.String("\n")).Or(Parse.String("\r"));

        static readonly Parser<string> Line =
            Elem(from xs in CommonSyntax.Comment.AnyComment.Or(Parse.Chars("\r\n").Select(c => c.ToString())).Other()
                 .Or(CommonSyntax.Comment.MultiLineComment.Return("")).Many()
                 from _ in CommonSyntax.Comment.SingleLineComment.Optional()
                 select string.Join("", xs));

        public static readonly Regex TextLineRegex = new(@"^( *[^# ].*$)", RegexOptions.Multiline);
        public static readonly Regex DirectiveRegex = new("^[\t ]*#(\\\\(\r\n|\n|\r)|.)*(\r\n|\n|\r)?", RegexOptions.Multiline);

        static readonly Parser<IPreprocessorDirective> Define =
            Directive("define",
                from name in Elem(ExpParser.IdentifirNoSpace)
                from value in Elem(Parse.Regex(@"(\\(\r\n|\n|\r)|[^\n\r])*").XOptional())
                select new Define(name, Regex.Replace(value.GetOrElse(""), @"\\(\r\n|\n|\r)", "", RegexOptions.Multiline))
            );

        static readonly Parser<IPreprocessorDirective> Include =
            Directive("include",
                from path in Elem(Parse.Regex("\".*\"|<.*>"))
                select new Include(path));

        static readonly Parser<IPreprocessorDirective> OtherDirective =
            DirectiveBegin(Elem(Line).Select(value => new OtherDirective(value)));

        static Parser<T> DirectiveBegin<T>(Parser<T> parser) {
            return
                from _1 in Parse.WhiteSpace.Many().Then(_ => Parse.Char('#'))
                from result in parser
                from _2 in EndOfLine.Optional()
                select result;
        }

        static Parser<T> Directive<T>(string name, Parser<T> parser) {
            return DirectiveBegin(Elem(Parse.String(name)).Then(_ => Elem(parser)).Named(name));
        }

        static Parser<T> Elem<T>(Parser<T> source) {
            var comment = CommonSyntax.Comment.MultiLineComment;
            var space = comment.Or(Parse.Chars("\t ").Many()).Many();
            return
                from _1 in space
                from x in source
                from _2 in space
                select x;
        }

        static Parser<IPreprocessorDirective> AllDirective =
            Define.Or(Include).Or(OtherDirective);

        static readonly Parser<IPreprocessorDirective> TextBlock =
            CommonSyntax.Comment.AnyComment.Return(" ")
            .Or(CommonSyntax.Comment.AnyComment.Return(' ').Or(Parse.Chars("\t ").Many().Then(_ => Parse.Char('#'))).Other())
            .Many()
            .Select(text => new TextBlock(string.Join("", text)/*CommonSyntax.Comment.AnyComment.Other()
                .Or(CommonSyntax.Comment.AnyComment.Return(""))
                .Many().Select(xs => string.Join("", xs))
                .Parse(text)*/));

        public static Parser<IEnumerable<IPreprocessorDirective>> PreprocessorDirective =
            AllDirective.Or(TextBlock)
            .Commented(CommonSyntax.Comment)
            .Select(x => x.Value)
            .XMany().Elem();
    }
}
