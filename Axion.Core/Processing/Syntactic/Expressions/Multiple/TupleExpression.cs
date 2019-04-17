using Axion.Core.Processing.CodeGen;
using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.TypeNames;
using Axion.Core.Specification;

namespace Axion.Core.Processing.Syntactic.Expressions.Multiple {
    /// <summary>
    ///     <c>
    ///         tuple_expr:
    ///             ['('] expr* [')']
    ///     </c>
    /// </summary>
    public class TupleExpression : MultipleExpression<Expression> {
        internal          bool     Expandable;
        internal override TypeName ValueType => Spec.TupleType();

        /// <summary>
        ///     Constructor for empty tuple.
        /// </summary>
        internal TupleExpression(
            SyntaxTreeNode parent,
            Token          start,
            Token          end
        ) {
            Parent      = parent;
            Expressions = new NodeList<Expression>(this);
            MarkPosition(start, end);
        }

        internal TupleExpression(
            SyntaxTreeNode       parent,
            bool                 expandable,
            NodeList<Expression> expressions
        ) {
            Parent      = parent;
            Expressions = expressions;
            Expandable  = expandable;
            if (Expressions.Count > 0) {
                MarkPosition(
                    Expressions[0],
                    Expressions[Expressions.Count - 1]
                );
            }
        }

        internal override void ToAxionCode(CodeBuilder c) {
            c.Write("(");
            c.AddJoin(", ", Expressions);
            c.Write(")");
        }

        internal override void ToCSharpCode(CodeBuilder c) {
            c.Write("(");
            c.AddJoin(", ", Expressions);
            c.Write(")");
        }
    }
}