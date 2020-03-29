using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.Operations;
using Axion.Core.Processing.Syntactic.Expressions.Postfix;
using static Axion.Core.Processing.Lexical.Tokens.TokenType;

namespace Axion.Core.Processing.Syntactic.Expressions.Common {
    /// <summary>
    ///     <c>
    ///         suffix-expr:
    ///         atom
    ///         {'|>' atom }
    ///         | ({ member | call-expr | index-expr } ['++' | '--']));
    ///     </c>
    /// </summary>
    public class PostfixExpr : PrefixExpr {
        protected PostfixExpr() { }

        protected PostfixExpr(Expr parent) : base(parent) { }

        internal new static PostfixExpr Parse(Expr parent) {
            TokenStream s = parent.Source.TokenStream;

            bool        unquoted = !s.PeekByIs(2, OpenParenthesis) && s.MaybeEat(Dollar);
            PostfixExpr value    = AtomExpr.Parse(parent);

            while (true) {
                Token exactPeek = s.ExactPeek;
                if (s.PeekIs(OpDot)) {
                    value = new MemberAccessExpr(parent, value).Parse();
                }
                // NOTE: this condition makes _impossible_ placement of
                //     function invocation parenthesis on the next line, e.g:
                //         x = func
                //         ()
                //     But this trick resolves conflicts when stmt
                //     starting with open paren is incorrectly treated
                //     as continuation of previous stmt.
                else if (s.PeekIs(OpenParenthesis) && s.Peek == exactPeek) {
                    value = new FuncCallExpr(parent, value).Parse(true);
                }
                else if (s.PeekIs(OpenBracket)) {
                    value = new IndexerExpr(parent, value).Parse();
                }
                else {
                    break;
                }
            }

            if (s.MaybeEat(OpIncrement, OpDecrement)) {
                var op = (OperatorToken) s.Token;
                op.Side = InputSide.Left;
                value   = new UnaryExpr(parent, op, value);
            }

            if (unquoted) {
                value = new CodeUnquotedExpr(parent, value);
            }

            return value;
        }
    }
}