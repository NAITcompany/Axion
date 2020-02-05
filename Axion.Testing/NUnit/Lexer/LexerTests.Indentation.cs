using System;
using Axion.Core.Source;
using NUnit.Framework;

namespace Axion.Testing.NUnit.Lexer {
    public partial class LexerTests {
        [Test]
        public void TestTabsIndentation() {
            SourceUnit src = MakeSourceFromCode(
                string.Join(
                    Environment.NewLine,
                    "i = 0",
                    "while i < 10:",
                    "\tj = 0",
                    "\twhile j < 5:",
                    "\t\tif i == 3 and j == 2:",
                    "\t\t\tConsole.print('Got it!')",
                    "\t\tj++",
                    "\ti++"
                )
            );
            Lex(src);
            Assert.AreEqual(0, src.Blames.Count);
        }

        [Test]
        public void TestSpacesIndentation() {
            SourceUnit src = MakeSourceFromCode(
                string.Join(
                    Environment.NewLine,
                    "i = 0",
                    "while i < 10:",
                    "    j = 0",
                    "    while j < 5:",
                    "        if i == 3 and j == 2:",
                    "            Console.print('Got it!')",
                    "        j++",
                    "    i++"
                )
            );
            Lex(src);
            Assert.AreEqual(0, src.Blames.Count);
        }

        [Test]
        public void TestWarnMixedIndentation() {
            SourceUnit src = MakeSourceFromCode(
                string.Join(
                    Environment.NewLine,
                    "i = 0",
                    "while i < 10:",
                    "\tj = 0",
                    "    while j < 5:",
                    "\t\tif i == 3 and j == 2:",
                    "            Console.print('Got it!')",
                    "\t\tj++",
                    "\ti++"
                )
            );
            Lex(src);
            // 2 blames for mixed indentation
            Assert.AreEqual(2, src.Blames.Count);
        }
    }
}