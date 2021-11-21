using Sprache;

namespace MFCFriendlyDriverGenerator {

    public static class CommonSyntax {
        public static readonly CommentParser Comment = new("//", "/*", "*/", "\n\r");

        /// <summary>
        /// 空白文字にマッチします。
        /// 常に空文字列を返します。
        /// </summary>
        static readonly Parser<string> IgnoreWhiteSpace =
            Parse.WhiteSpace.Return("");

        /// <summary>
        /// コメントアウトと不要な食う文字列ににマッチします。
        /// 常に空文字列を返します。
        /// </summary>
        static readonly Parser<string> UnnecessaryString =
            IgnoreWhiteSpace.Or(Comment.AnyComment).Many()
            .Return("");

        public static Parser<T> BeginEnd<T>(this Parser<T> bodyParser) {
            return bodyParser.Contained(Parse.String("BEGIN").Elem(), Parse.String("END").Elem());
        }

        public static readonly Parser<char> CommaSeparator =
            Parse.Char(',').Elem();

        /// <summary>
        ///  指定したパーサーの前後に空白やコメントが入ってもいいようにします。
        /// </summary>
        public static Parser<T> Elem<T>(this Parser<T> parser) {
            return parser.Contained(UnnecessaryString, UnnecessaryString);
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
