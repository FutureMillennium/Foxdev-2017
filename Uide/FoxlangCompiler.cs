using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uide
{
	class FoxlangCompiler
	{
		enum ReadingState { Normal, IgnoringUntilNewLine, ReadingString }
		enum ParsingState { HashCompile, HashCompileBlock }

		public class Token
		{
			public string token;
			public int line,
				col,
				pos;
		}

		public class OutputMessage
		{
			public enum MessageType { Warning, Error }

			public MessageType type;
			public string message;
			public Token token;
			public int errorNumber;
		}

		public class Project
		{
			public enum Format { Invalid, Flat }

			public string name,
				entryPoint,
				output;
			public Format format;
			public List<string> files,
				run;
		}

		public List<Token> tokens = new List<Token>();
		public List<OutputMessage> outputMessages = new List<OutputMessage>();
		public List<Project> projects = new List<Project>();
		public string projectName;
		Project curProject;

		public void Compile(string filePath)
		{
			#region Lexical parsing
			try
			{
				projectName = Path.GetFileNameWithoutExtension(filePath);
				StreamReader streamReader = File.OpenText(filePath);

				char c, prevC = (char)0;
				int pos = 0, line = 1, col = 1;
				Token currentToken = null;
				ReadingState readingState = ReadingState.Normal;


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
					c = (char)streamReader.Read();

					switch (readingState)
					{
						case ReadingState.IgnoringUntilNewLine:
							if (c == '\n')
							{
								line++;
								col = 0;
								readingState = ReadingState.Normal;
							}
							break;
						case ReadingState.ReadingString:
							currentToken.token += c;
							if (c == '\n')
							{
								line++;
								col = 0;
							}

							if (c == '\'' && prevC != '\\')
							{
								readingState = ReadingState.Normal;
								AddSymbol();
							}
							break;
						case ReadingState.Normal:
							switch (c)
							{
								case '/':
									if (currentToken != null && currentToken.token == "/")
									{
										currentToken = null;
										readingState = ReadingState.IgnoringUntilNewLine;
									}
									else goto BreakingSymbol;
									break;
								case '\'':
									readingState = ReadingState.ReadingString;
									goto BreakingSymbol;
								case ';':
								case '(':
								case ')':
								BreakingSymbol:
									AddSymbol();
									goto default;
								case ' ':
								case '\t':
									AddSymbol();
									break;
								case '\r':
									col--;
									AddSymbol();
									break;
								case '\n':
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
									break;
							}
							break;
					}

					pos++;
					col++;
					prevC = c;
				}
			}
			catch (Exception e)
			{
				// @TODO
			}

			// @TODO check invalid state (e.g. remaining in ParsingString)
			#endregion

			Stack<ParsingState> parsingStateStack = new Stack<ParsingState>();

			foreach (Token tok in tokens)
			{
				string token = tok.token;

				if (parsingStateStack.Count > 0)
				{
					ParsingState state = parsingStateStack.Peek();

					switch (state)
					{
						case ParsingState.HashCompile:
							if (token == "{")
							{
								parsingStateStack.Push(ParsingState.HashCompileBlock);
							}
							else
							{
								if (token[0] == '#')
								{
									// @TODO cleanup
									if (token == "#projectName")
										curProject.name = projectName;
									else
									{
										outputMessages.Add(new OutputMessage
										{
											type = OutputMessage.MessageType.Error,
											message = "Not implemented.",
											token = tok,
											errorNumber = 10001,
										});
										return;
									}
								}
								else if (token[0] == '\'')
								{
									curProject.name = token; // @TODO parse string
								}
								else
								{
									outputMessages.Add(new OutputMessage
									{
										type = OutputMessage.MessageType.Error,
										message = "Can't accept.",
										token = tok,
										errorNumber = 10000,
									});
									return;
								}
							}
							break;

						case ParsingState.HashCompileBlock:
							// @TODO
							break;
					}

				}
				else
				{
					switch (token)
					{
						case "#Compile":
							curProject = new Project();
							projects.Add(curProject);

							parsingStateStack.Push(ParsingState.HashCompile);
							break;
					}
				}
			}
		}
	}
}
