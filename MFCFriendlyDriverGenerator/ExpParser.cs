using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Sprache;

namespace MFCFriendlyDriverGenerator {

    public interface IExp { }

    /// <summary>
    ///  OPには+や-が入ります
    /// </summary>
    public record UnaryOperator(string OP, IExp Exp) : IExp;

    /// <summary>
    ///  左辺優先
    /// </summary>
    public record BinOperator(string OP, IExp Left, IExp Right) : IExp;
    public record Identifier(string Name) : IExp;
    public record StringLiteral(string Str) : IExp;
    public record IntegerLiteral(int Value) : IExp;

    public static class ExpParser {
        public static readonly Parser<string> StringLiteralBody =
            Parse.Regex("(\\\\.|[^\"])*").Select(Regex.Unescape);
        public static readonly Parser<string> StringLiteral =
            Pair(Parse.Char('"').Elem(), Parse.Char('"').Elem(),
                 StringLiteralBody)
            .Elem();
        public static readonly Parser<string> IdentifirNoSpace = Parse.Regex("[a-zA-Z_][a-zA-Z_0-9]*", "identifier");
        public static readonly Parser<string> Identifier = IdentifirNoSpace.Elem();

        static readonly Parser<int> HexLiteral =
            Parse.Regex("0x[0-9A-Fa-f]+").Elem()
            .Select(hex => int.Parse(hex.Substring(2), NumberStyles.HexNumber));

        public static readonly Parser<int> IntLiteral =
            from v in HexLiteral.Or(Parse.Decimal.Select(int.Parse))
            from isLong in Parse.Char('L').Optional()
            select v;

        static Parser<IExp> BinaryExp(Parser<IEnumerable<char>> opParser, Parser<IExp> expParser) {
            return BinaryExp(opParser.Text(), expParser);
        }

        static Parser<IExp> BinaryExp(Parser<string> opParser, Parser<IExp> expParser)
        {
            return
                from leftExp in expParser.Elem()
                from rights in (from op in opParser.Elem()
                                from rightExp in expParser.Elem()
                                select (op, rightExp)).Many()
                let exps = rights.ToArray()
                select exps.Any()
                ? exps.Skip(1).Aggregate(new BinOperator(exps[0].op, leftExp, exps[0].rightExp),
                                 (left, x) => new BinOperator(x.op, left, x.rightExp))
                : leftExp;
        }

        public static Parser<T3> Pair<T1, T2, T3>(Parser<T1> beginParser, Parser<T2> endParser, Parser<T3> bodyParser) {
            return
                from _1 in beginParser
                from body in bodyParser
                from _2 in endParser
                select body;
        }

        public static Parser<T> Paren<T>(this Parser<T> bodyParser) {
            return Pair(Parse.Char('('), Parse.Char(')'), bodyParser);
        }

        static readonly Parser<IExp> ArithmeticExpression =
            BinaryExp(Parse.Chars("+-").Select(c => c.ToString()), BinaryExp(Parse.Chars("*/").Select(op => op.ToString()),
                                                                             (from op in Parse.Regex("NOT|!|-|~").Elem()
                                                                              from exp in Parse.Ref(() => UnsignedFactor)
                                                                              select new UnaryOperator(op, exp)
                                                                             )
                                                                             .Or(Parse.Ref(() => UnsignedFactor))));

        static readonly Parser<IExp> LogicalFactor =
            BinaryExp(Parse.Regex("==|!=|<|>|<=|>="), ArithmeticExpression);

        static readonly Parser<IExp> BitExp =
            BinaryExp(Parse.String("|"), BinaryExp(Parse.String("&"), LogicalFactor));

        public static readonly Parser<IExp> Exp = BinaryExp(Parse.String("||"), BinaryExp(Parse.String("&&"), BitExp));

        static readonly Parser<IExp> UnsignedFactor =
            Identifier.Select(name => new Identifier(name)).Or(
                Exp.Paren()).Or(
                    IntLiteral.Elem().Select(integer => new IntegerLiteral(integer))
                ).Or(
                    StringLiteral.Select(str => new StringLiteral(str))
                );
    }

}
