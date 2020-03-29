using System.Linq;
using Axion.Core.Processing.Errors;
using Axion.Core.Processing.Lexical.Tokens;
using Axion.Core.Processing.Syntactic.Expressions.Atomic;
using Axion.Core.Processing.Syntactic.Expressions.Common;

namespace Axion.Core.Processing.Syntactic.Expressions.Postfix {
    public sealed class FuncCallArg : Expr {
        private NameExpr name;

        public NameExpr Name {
            get => name;
            set => name = Bind(value);
        }

        private Expr val;

        public Expr Value {
            get => val;
            set => val = Bind(value);
        }

        internal FuncCallArg(
            Expr?     parent = null,
            NameExpr? name   = null,
            Expr?     value  = null
        ) : base(
            parent
         ?? GetParentFromChildren(name, value)
        ) {
            Name  = name;
            Value = value;
            MarkStart(Name);
            MarkEnd(Value);
        }

        /// <summary>
        ///     <c>
        ///         multiple-arg:
        ///             comprehension
        ///             | ({ argument ',' }
        ///                (argument [',']
        ///                | '*' expr [',' '**' expr]
        ///                | '**' expr ));
        ///         argument:
        ///             expr ['=' expr];
        ///     </c>
        /// </summary>
        internal static NodeList<FuncCallArg> ParseArgList(
            Expr         parent,
            FuncCallArg? first          = null,
            bool         allowGenerator = false
        ) {
            var args = new NodeList<FuncCallArg>(parent);

            if (first != null) {
                args.Add(first);
            }

            if (parent.Stream.PeekIs(TokenType.CloseParenthesis)) {
                return args;
            }

            while (true) {
                FuncCallArg arg;
                // named arg
                if (parent.Stream.PeekIs(TokenType.Identifier) && parent.Stream.PeekByIs(2, TokenType.OpAssign)) {
                    var argName = (NameExpr) AtomExpr.Parse(parent);
                    parent.Stream.Eat(TokenType.OpAssign);
                    InfixExpr argValue = InfixExpr.Parse(parent);
                    arg = new FuncCallArg(parent, argName, argValue);
                    if (args.Any(a => a.Name.ToString() == argName.ToString())) {
                        LangException.Report(BlameType.DuplicatedNamedArgument, arg);
                    }
                }
                else {
                    Expr argValue = InfixExpr.Parse(parent);
                    // generator arg
                    if (parent.Stream.PeekIs(TokenType.KeywordFor)) {
                        arg = new FuncCallArg(
                            parent,
                            value: new ForComprehension(parent, argValue) { IsGenerator = true }.Parse()
                        );
                    }
                    else {
                        // TODO: star args
                        parent.Stream.MaybeEat(TokenType.OpMultiply);
                        arg = new FuncCallArg(parent, value: argValue);
                    }
                }

                args.Add(arg);
                if (!parent.Stream.MaybeEat(TokenType.Comma)) {
                    break;
                }
            }

            return args;
        }
    }
}