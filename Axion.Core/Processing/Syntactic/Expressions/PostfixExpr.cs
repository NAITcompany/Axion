using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.Atomic;
using Axion.Core.Processing.Syntactic.Expressions.Operations;
using Axion.Core.Processing.Syntactic.Expressions.Postfix;
using static Axion.Core.Processing.Lexical.Tokens.TokenType;

namespace Axion.Core.Processing.Syntactic.Expressions {
    /// <summary>
    ///     <c>
    ///         suffix_expr:
    ///         atom
    ///         {'|>' atom }
    ///         | ({ member | call_expr | index_expr } ['++' | '--']));
    ///     </c>
    /// </summary>
    public static class PostfixExpr {
        internal static Expr Parse(Expr parent) {
            TokenStream s = parent.Source.TokenStream;

            bool unquoted = s.MaybeEat(Dollar);
            Expr value    = AtomExpr.Parse(parent);
            if (value is IDefinitionExpr) {
                return value;
            }

            var loop = true;
            while (loop) {
                Token exactPeek = s.ExactPeek;
                switch (s.Peek.Type) {
                case OpDot:
                    value = new MemberAccessExpr(parent, value).Parse();
                    break;

                /* NOTICE:
                condition makes _impossible_ newline
                placement of function invocation braces
                e.g:
                x = func
                ()
                but this resolves conflicts when stmts that
                start with open paren are treated as
                continuation of previous stmt.
                */
                case OpenParenthesis when s.Peek == exactPeek:
                    value = new FuncCallExpr(parent, value).Parse(true);
                    break;

                case OpenBracket:
                    value = new IndexerExpr(parent, value).Parse();
                    break;

                default:
                    loop = false;
                    break;
                }
            }

            if (s.MaybeEat(OpIncrement, OpDecrement)) {
                var op = (OperatorToken) s.Token;
                op.Side = InputSide.Right;
                value   = new UnaryExpr(parent, op, value);
            }

            if (unquoted) {
                value = new CodeUnquotedExpr(parent, value);
            }

            return value;
        }
    }
}