using Axion.Core.Processing.Syntactic.Expressions.Common;
using static Axion.Specification.TokenType;

namespace Axion.Core.Processing.Syntactic.Expressions.Postfix {
    /// <summary>
    ///     <c>
    ///         func-call-expr:
    ///             atom '(' [multiple-arg | (arg for-comprehension)] ')';
    ///     </c>
    /// </summary>
    public class FuncCallExpr : PostfixExpr {
        private Node target = null!;

        public Node Target {
            get => target;
            set => target = Bind(value);
        }

        private NodeList<FuncCallArg>? args;

        public NodeList<FuncCallArg> Args {
            get => InitIfNull(ref args);
            set => args = Bind(value);
        }

        public FuncCallExpr(Node parent) : base(parent) { }

        public FuncCallExpr Parse(bool allowGenerator = false) {
            Stream.Eat(OpenParenthesis);
            Args = FuncCallArg.ParseArgList(
                this,
                allowGenerator: allowGenerator
            );
            End = Stream.Eat(CloseParenthesis).End;
            return this;
        }
    }
}
