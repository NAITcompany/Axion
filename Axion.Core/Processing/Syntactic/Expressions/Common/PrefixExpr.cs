using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.Operations;
using Axion.Core.Specification;

namespace Axion.Core.Processing.Syntactic.Expressions.Common {
    /// <summary>
    ///     "Prefix" expression is any <see cref="PostfixExpr"/>
    ///     coming after any count of allowed prefix operators.
    ///     <br/>
    ///     (e.g ++++++!x is valid expression)
    ///     <c>
    ///         prefix-expr:
    ///             (PREFIX-OPERATOR prefix) | postfix;
    ///     </c>
    /// </summary>
    public class PrefixExpr : InfixExpr {
        protected PrefixExpr() { }

        protected PrefixExpr(Node parent) : base(parent) { }

        internal new static PrefixExpr Parse(Node parent) {
            TokenStream s = parent.Source.TokenStream;

            if (s.MaybeEat(Spec.PrefixOperators)) {
                var op = (OperatorToken) s.Token;
                op.Side = InputSide.Right;
                return new UnaryExpr(parent) {
                    Operator = op, Value = Parse(parent)
                };
            }

            return PostfixExpr.Parse(parent);
        }
    }
}