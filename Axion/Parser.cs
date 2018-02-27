﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Axion.Tokens;

namespace Axion
{
	internal class Parser
	{
		private readonly List<TokenType> EndTokenTypes = new List<TokenType>();
		private readonly List<Token> Tokens;
		public readonly List<Token> SyntaxTree = new List<Token>();
		private int TokenIndex;

		internal Parser(List<Token> tokens, params TokenType[] endTokenTypes)
		{
			Tokens = tokens;
			EndTokenTypes.AddRange(endTokenTypes);
		}

		internal void Parse()
		{
			for (; TokenIndex < Tokens.Count; TokenIndex++)
			{
				var expression = NextExpression(null);
				if (expression != null)
				{
					SyntaxTree.Add(expression);
				}
			}
		}

		private Token NextExpression(Token previousToken)
		{
			if (TokenIndex >= Tokens.Count)
			{
				return previousToken;
			}

			var token = Tokens[TokenIndex];
			var type = token.Type;

			if (EndTokenTypes.Contains(type))
			{
				return previousToken;
			}

			TokenIndex++;
			// Number, String, Identifier
			if (previousToken is null && (type.ToString("G").ToLower().StartsWith("number") ||
										  type == TokenType.String ||
										  type == TokenType.Identifier))
			{
				return NextExpression(token);
			}
			// Operation
			if (type == TokenType.Operator)
			{
				// get right operand
				var nextToken = NextExpression(null);
				return NextExpression(new OperationToken(token.Value, previousToken, nextToken));
			}
			// Function call
			if (type == TokenType.OpenParenthese && previousToken?.Type == TokenType.Identifier)
			{
				var arguments = MultipleExpressions(TokenType.Comma, TokenType.CloseParenthese);
				return NextExpression(new FunctionCallToken(previousToken, arguments));
			}

			Program.LogError("ERROR: Unknown token type: " + type.ToString("G"));
			return NextExpression(token);
		}

		private List<Token> MultipleExpressions(TokenType separatorType, TokenType endTokenType)
		{
			var ret = new List<Token>();
			var type = Tokens[TokenIndex].Type;
			if (type == endTokenType) // when 'call()'
			{
				TokenIndex++;
			}
			else
			{
				EndTokenTypes.Add(separatorType);
				EndTokenTypes.Add(endTokenType);
				while (type != endTokenType)
				{
					var token = NextExpression(null);
					if (token != null && token.Type != separatorType && token.Type != endTokenType)
					{
						ret.Add(token);
					}
					type = Tokens[TokenIndex].Type;
					TokenIndex++;
					if (TokenIndex >= Tokens.Count) break;
				}
				EndTokenTypes.Remove(endTokenType);
				EndTokenTypes.Remove(separatorType);
			}
			return ret;
		}

		public void SaveFile(string fileName)
		{
			if (File.Exists(fileName))
			{
				File.Delete(fileName);
			}

			File.WriteAllLines(fileName, SyntaxTree.Select(token => token?.ToString(0)));
		}
	}
}
