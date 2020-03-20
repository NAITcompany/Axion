using Axion.Core.Processing.CodeGen;
using Axion.Core.Processing.Syntactic.Expressions.Atomic;
using static Axion.Core.Processing.Lexical.Tokens.TokenType;

namespace Axion.Core.Processing.Syntactic.Expressions.Statements {
    /// <summary>
    ///     <c>
    ///         continue-expr:
    ///             'continue' [name];
    ///     </c>
    /// </summary>
    public class ContinueExpr : Expr {
        private NameExpr loopName;

        public NameExpr LoopName {
            get => loopName;
            set => SetNode(ref loopName, value);
        }

        public ContinueExpr(
            Expr?     parent   = null,
            NameExpr? loopName = null
        ) : base(
            parent
         ?? GetParentFromChildren(loopName)
        ) {
            LoopName = loopName;
        }

        public ContinueExpr Parse() {
            SetSpan(
                () => {
                    Stream.Eat(KeywordContinue);
                    if (Stream.PeekIs(Identifier)) {
                        LoopName = new NameExpr(this).Parse();
                    }
                }
            );
            return this;
        }

        public override void ToAxion(CodeWriter c) {
            c.Write("continue");
            if (LoopName != null) {
                c.Write(" ", LoopName);
            }
        }

        public override void ToCSharp(CodeWriter c) {
            c.Write("continue");
        }

        public override void ToPython(CodeWriter c) {
            c.Write("continue");
        }
    }
}