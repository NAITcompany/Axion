using System.Diagnostics;
using System.Linq;
using Axion.Specification;

namespace Axion.Core.Processing.Lexical {
    /// <summary>
    ///     Character stream with possibility
    ///     to peek next char, rest of line,
    ///     and moving backwards.
    /// </summary>
    public class TextStream {
        int charIdx = -1;
        int lineIdx;
        int columnIdx;

        public string Text { get; }

        /// <summary>
        ///     0-based (Line, Column) position of character in source code.
        /// </summary>
        public Location Location => new(lineIdx, columnIdx);

        /// <summary>
        ///     Checks that next character is line/source terminator.
        /// </summary>
        public bool AtEndOfLine =>
            Spec.Eols.Contains(Peek()) || PeekIs(Spec.EndOfCode);

        public string RestOfLine {
            get {
                var textFromCurrent = Text[(charIdx + 1)..];
                var i = textFromCurrent.IndexOf('\n');
                if (i == -1) {
                    return textFromCurrent;
                }

                return textFromCurrent[..(i + 1)];
            }
        }

        /// <summary>
        ///     Current (eaten) character.
        /// </summary>
        public char Char => charIdx < 0 ? Spec.EndOfCode : Text[charIdx];

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text);

        public TextStream(string text) {
            if (!text.EndsWith(Spec.EndOfCode)) {
                text += Spec.EndOfCode;
            }

            Text = text;
        }

        /// <summary>
        ///     Returns next character from stream,
        ///     not eating it.
        /// </summary>
        public char Peek() {
            if (charIdx + 2 >= 0 && charIdx + 2 < Text.Length) {
                return Text[charIdx + 1];
            }

            return Spec.EndOfCode;
        }

        /// <summary>
        ///     Returns next N characters from stream,
        ///     not eating them.
        /// </summary>
        public string Peek(int length) {
            if (charIdx + 1 + length >= 0
             && charIdx + 1 + length < Text.Length) {
                return Text.Substring(charIdx + 1, length);
            }

            return new string(Spec.EndOfCode, length);
        }

        /// <summary>
        ///     Compares next substring from stream
        ///     with expected strings.
        /// </summary>
        public bool PeekIs(params string[] expected) {
            // TODO: resolve bottleneck (select peek pieces to element of each unique size?)
            return expected.Any(s => s == Peek(s.Length));
        }

        public bool PeekIs(params char[] expected) {
            return expected.Contains(Peek());
        }

        /// <summary>
        ///     Consumes next substring from stream,
        ///     checking that it's equal to expected.
        /// </summary>
        public string? Eat(params string[] expected) {
            if (expected.Length == 0) {
                Move();
                return Char.ToString();
            }

            foreach (var value in expected) {
                var nxt = Peek(value.Length);
                if (nxt == value) {
                    Move(nxt.Length);
                    return nxt;
                }
            }

            return null;
        }

        /// <summary>
        ///     Consumes next char from stream,
        ///     checking that it's equal to expected.
        /// </summary>
        public string? Eat(params char[] expected) {
            if (expected.Length == 0) {
                Move();
                return Char.ToString();
            }

            var nxt = Peek();
            if (expected.Contains(nxt)) {
                Move();
                return nxt.ToString();
            }

            return null;
        }

        void Move(int by = 1) {
            Debug.Assert(by > 0);
            while (by > 0 && Peek() != Spec.EndOfCode) {
                if (Peek() == '\n') {
                    lineIdx++;
                    columnIdx = 0;
                }
                else {
                    columnIdx++;
                }

                charIdx++;
                by--;
            }
        }
    }
}
