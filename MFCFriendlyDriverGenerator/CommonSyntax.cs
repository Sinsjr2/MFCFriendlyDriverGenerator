using Sprache;

namespace MFCFriendlyDriverGenerator {

    public static class CommonSyntax {
        public static readonly CommentParser Comment = new("//", "/*", "*/", "\n\r");

        public static Parser<T> BeginEnd<T>(this Parser<T> bodyParser) {
            return ExpParser.Pair(Parse.String("BEGIN").Elem(), Parse.String("END").Elem(), bodyParser);
        }

        public static readonly Parser<char> CommaSeparator =
            Parse.Char(',').Elem();

        /// <summary>
        ///  指定したパーサーの前後に空白やコメントが入ってもいいようにします。
        /// </summary>
        public static Parser<T> Elem<T>(this Parser<T> parser) {
            var space = Parse.Chars("\r\n\t ").Select(_ => "");
            var comments = Comment.AnyComment;
            var unnecessaryString = space.Or(comments).Many();
            return
                from _1 in unnecessaryString
                from x in parser
                from _2 in unnecessaryString
                select x;
        }

        /// <summary>
        /// 指定したパーサー以外の文字列にマッチし、その文字列を返します。
        /// </summary>
        public static Parser<string> Other<T>(this Parser<T> parser) {
            var notParser = Parse.Not(parser);
            return
                from _1 in notParser
                from heads in Parse.AnyChar.Then(c => notParser.Return(c)).Many().Text()
                from last in Parse.AnyChar.Optional()
                select last.IsDefined ? heads + last.GetOrDefault().ToString() : heads;
        }
    }
}
