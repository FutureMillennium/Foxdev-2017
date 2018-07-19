using System.Collections.Generic;
using System.IO;

namespace Compiler
{
	partial class LexerParser
	{
		enum LexingState { Normal, IgnoringUntilNewLine, ReadingString, ReadingDoubleString, NestedComments }

		internal class Token
		{
			public string token;
			public int line,
				col,
				pos;

			public override string ToString()
			{
				return this.token + " \t[line: " + this.line + ", col: " + this.col + "]";
			}
		}

		static internal void LexerParse(string filePath, List<Token> tokens)
		{
			try
			{
				StreamReader streamReader = File.OpenText(filePath);

				char c, prevC = (char)0;
				int pos = 0, line = 1, col = 1;
				Token currentToken = null;
				LexingState readingState = LexingState.Normal;
				int nestedCommentLevel = 0;


				void AddSymbol()
				{
					if (currentToken != null && currentToken.token.Length > 0)
					{
						tokens.Add(currentToken);
						currentToken = null;
					}
				}


				while (streamReader.EndOfStream == false)
				{
					bool addImmediately = false;

					c = (char)streamReader.Read();

					switch (readingState)
					{
						case LexingState.NestedComments:
							if (c == '\n') // @TODO cleanup
							{
								line++;
								col = 0;
							}

							if (c == '*' && prevC == '/')
							{
								nestedCommentLevel++;
							}
							else if (c == '/' && prevC == '*')
							{
								if (nestedCommentLevel == 0)
								{
									readingState = LexingState.Normal;
								}
								else
								{
									nestedCommentLevel--;
								}
							}

							break;
						case LexingState.IgnoringUntilNewLine:
							if (c == '\n') // @TODO cleanup
							{
								line++;
								col = 0;
								readingState = LexingState.Normal;
							}
							break;
						case LexingState.ReadingDoubleString:
							currentToken.token += c;
							if (c == '\n') // @TODO cleanup
							{
								line++;
								col = 0;
							}

							if (c == '"' && prevC != '\\')
							{
								readingState = LexingState.Normal;
								AddSymbol();
							}
							break;
						case LexingState.ReadingString:
							currentToken.token += c;
							if (c == '\n') // @TODO cleanup
							{
								line++;
								col = 0;
							}

							if (c == '\'' && prevC != '\\')
							{
								readingState = LexingState.Normal;
								AddSymbol();
							}
							break;
						case LexingState.Normal:
							switch (c)
							{
								case '/':
									if (currentToken != null && currentToken.token == "/")
									{
										currentToken = null;
										readingState = LexingState.IgnoringUntilNewLine;
									}
									else goto BreakingSymbol;
									break;
								case '*':
									if (currentToken != null && currentToken.token == "/")
									{
										currentToken = null;
										nestedCommentLevel = 0;
										c = (char)0; // in case of "/*/"
										readingState = LexingState.NestedComments;
									}
									else goto AddImmediately;
									break;
								case '"':
									readingState = LexingState.ReadingDoubleString;
									goto BreakingSymbol;
								case '\'':
									readingState = LexingState.ReadingString;
									goto BreakingSymbol;
								case '+':
									if (currentToken != null && currentToken.token == "+")
										goto default;
									else
										goto BreakingSymbol;
								case ';':
								case '(':
								case ')':
								case '[':
								case ']':
								case ':':
								case ',':
									AddImmediately:
									addImmediately = true;
									BreakingSymbol:
									AddSymbol();
									goto default;
								case ' ':
								case '\t':
								case '\r': // @TODO assuming \r\n, not \r on its own
									AddSymbol();
									break;
								case '\n': // @TODO cleanup
									line++;
									col = 0;
									AddSymbol();
									break;
								default:
									if (currentToken == null)
									{
										currentToken = new Token
										{
											token = "",
											line = line,
											col = col,
											pos = pos,
										};
									}
									currentToken.token += c;
									if (addImmediately)
									{
										addImmediately = false;
										AddSymbol();
									}
									break;
							}
							break;
					}

					pos++;
					col++;
					prevC = c;
				}

				streamReader.Close();
			}
			catch
			{
				// @TODO
			}

			// @TODO check invalid state (e.g. remaining in ParsingString)
		}
	}
}
