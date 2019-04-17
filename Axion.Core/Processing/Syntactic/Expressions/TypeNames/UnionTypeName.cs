using System;
using Axion.Core.Processing.CodeGen;
using Axion.Core.Specification;

namespace Axion.Core.Processing.Syntactic.Expressions.TypeNames {
    /// <summary>
    ///     <c>
    ///         union_type:
    ///             type ('|' type)+
    ///     </c>
    /// </summary>
    public class UnionTypeName : TypeName {
        private TypeName left;

        public TypeName Left {
            get => left;
            set => SetNode(ref left, value);
        }

        private TypeName right;

        public TypeName Right {
            get => right;
            set => SetNode(ref right, value);
        }

        /// <summary>
        ///     Constructs new <see cref="UnionTypeName"/> from Axion tokens.
        /// </summary>
        public UnionTypeName(SyntaxTreeNode parent, TypeName left) {
            Parent = parent;
            Left   = left;

            MarkStart(Left);

            Eat(TokenType.OpBitOr);
            Right = ParseTypeName(this);

            MarkEnd(Token);
        }

        /// <summary>
        ///     Constructs plain <see cref="UnionTypeName"/> without position in source.
        /// </summary>
        public UnionTypeName(TypeName left, TypeName right) {
            Left  = left;
            Right = right;
            MarkPosition(Left, Right);
        }

        internal override void ToAxionCode(CodeBuilder c) {
            c.Write(Left, " | ", Right);
        }

        internal override void ToCSharpCode(CodeBuilder c) {
            throw new NotSupportedException();
        }
    }
}