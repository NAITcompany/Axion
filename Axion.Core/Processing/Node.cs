using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Axion.Core.Processing.Syntactic;
using Axion.Core.Processing.Syntactic.Expressions;
using Axion.Core.Processing.Syntactic.Expressions.TypeNames;
using Axion.Core.Processing.Traversal;
using Axion.Core.Source;
using Newtonsoft.Json;

namespace Axion.Core.Processing {
    /// <summary>
    ///     Span of source code / Tree leaf with parent and children nodes.
    /// </summary>
    public class Node {
        [JsonIgnore]
        public Unit Source { get; private protected set; }

        /// <summary>
        ///     Start location of this node's code span.
        /// </summary>
        public Location Start { get; private protected set; }

        /// <summary>
        ///     End location of this node's code span.
        /// </summary>
        public Location End { get; private protected set; }

        private Ast ast = null!;

        /// <summary>
        ///     Abstract Syntax Tree root of this node.
        ///     <exception cref="NullReferenceException">
        ///         Thrown if node is not completely bound at the moment.
        ///     </exception>
        /// </summary>
        internal Ast Ast {
            get {
                if (ast != null) {
                    return ast;
                }

                Node? p = this;
                while (!(p is Ast)) {
                    p = p?.Parent
                     ?? throw new NullReferenceException(
                            "Cannot get AST for non-completely bound node"
                        );
                }

                ast = (Ast) p;
                return ast;
            }
        }

        private TypeName valueType = null!;

        /// <summary>
        ///     Language type-name of this node that can be inferred from context.
        /// </summary>
        [JsonIgnore]
        [NoPathTraversing]
        public virtual TypeName ValueType {
            get => valueType;
            protected internal set => valueType = Bind(value);
        }

        /// <summary>
        ///     Direct reference to the attribute of
        ///     parent to which this node is bound.
        /// </summary>
        internal ITreePath Path = null!;

        /// <summary>
        ///     Reference to parent of this node.
        /// </summary>
        [NoPathTraversing]
        protected internal Node? Parent { get; set; }

        public Node(Unit source, Location start = default, Location end = default) {
            Source = source;
            Start  = start;
            End    = end;
        }

        /// <summary>
        ///     Returns first parent of this node with given type.
        ///     (<code>null</code> if parent of given type is not exists).
        /// </summary>
        internal T? GetParent<T>()
            where T : Node {
            Node? p = this;
            while (true) {
                p = p.Parent;
                if (p == null || p is T) {
                    return (T?) p;
                }

                if (p is Ast) {
                    return null;
                }
            }
        }

        protected NodeList<T> InitIfNull<T>(ref NodeList<T> list)
            where T : Node {
            if (list == null) {
                list = new NodeList<T>(this);
            }
            list = Bind(list);
            return list;
        }

        #region Node binding methods

        /// <summary>
        ///     [ONLY-INSIDE-PROPERTY]
        ///     Binds given property value to this node and extends it's span if needed.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if provided property value is null.
        /// </exception>
        protected T Bind<T>(T value, [CallerMemberName] string propertyName = "")
            where T : Node {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            return BindNode(value, propertyName);
        }

        /// <summary>
        ///     [ONLY-INSIDE-PROPERTY]
        ///     Binds given property value to this node and extends it's span if needed.
        /// </summary>
        protected T? BindNullable<T>(T? value, [CallerMemberName] string propertyName = "")
            where T : Node {
            if (value == null) {
                return value;
            }
            return BindNode(value, propertyName);
        }

        /// <summary>
        ///     Internal node binding method.
        ///     Creates a path to parent attribute.
        /// </summary>
        private T BindNode<T>(T value, string propertyName)
            where T : Node {
            ExtendSpan(value);

            value.Parent = this;
            value.Path   = new NodeTreePath(value, GetType().GetProperty(propertyName));

            return value;
        }

        /// <summary>
        ///     Binds given node to this node and extends parent span if needed.
        /// </summary>
        public T Bind<T>(T value, NodeList<T> list, int index)
            where T : Node {
            ExtendSpan(value);

            value.Parent = this;
            value.Path   = new NodeListTreePath<T>(list, index);

            return value;
        }

        /// <summary>
        ///     [ONLY-INSIDE-PROPERTY]
        ///     Binds given property value to this node and extends it's span if needed.
        /// </summary>=
        /// <exception cref="ArgumentNullException">
        ///     Thrown if provided property value is null.
        /// </exception>
        protected NodeList<T> Bind<T>([NotNull] NodeList<T> list)
            where T : Node {
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            if (list.Count == 0) {
                return new NodeList<T>(this);
            }
            ExtendSpan(list[0], list[^1]);

            for (var i = 0; i < list.Count; i++) {
                if (list[i] is Node n) {
                    n.Parent = this;
                    n.Path   = new NodeListTreePath<T>(list, i);
                }
            }
            return list;
        }

        #endregion

        #region Location marking methods

        private bool firstTimeSpanMarking = true;

        /// <summary>
        ///     Extends this span of code if provided mark is out of existing span.
        /// </summary>
        private void ExtendSpan(Node n) {
            // if span is marked first time, set span equal to starting one
            // to prevent new node spanning from (1,1) to end.
            if (firstTimeSpanMarking) {
                Start                = n.Start;
                End                  = n.End;
                firstTimeSpanMarking = false;
                return;
            }
            if (n.Start < Start) {
                Start = n.Start;
            }
            if (n.End > End) {
                End = n.End;
            }
            // fix negative span
            if (End < Start) {
                End = Start;
            }
        }

        /// <summary>
        ///     Extends this span of code if any of provided marks is out of existing span.
        /// </summary>
        private void ExtendSpan(Node a, Node b) {
            // if span is marked first time, select least span of a & b.
            // to prevent new node spanning from (1,1) to end.
            if (firstTimeSpanMarking) {
                Start                = Location.Max(a.Start, b.Start);
                End                  = Location.Min(a.End, b.End);
                firstTimeSpanMarking = false;
                return;
            }
            if (a.Start < Start) {
                Start = a.Start;
            }
            else if (b.Start < Start) {
                Start = b.Start;
            }
            if (b.End > End) {
                End = b.End;
            }
            else if (a.End > End) {
                End = a.End;
            }
            // fix negative span
            if (End < Start) {
                End = Start;
            }
        }

        #endregion

        public override string ToString() {
            return "from " + Start + " to " + End;
        }
    }
}