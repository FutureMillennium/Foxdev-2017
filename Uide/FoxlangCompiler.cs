﻿using System;
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
		enum ParsingState { HashCompile, HashCompileBlock, ComposeString, OutputProjectAssign }

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

				streamReader.Close();
			}
			catch (Exception e)
			{
				// @TODO
			}

			// @TODO check invalid state (e.g. remaining in ParsingString)
			#endregion

			

			Stack<ParsingState> parsingStateStack = new Stack<ParsingState>();
			Stack<string> stringDataStack = new Stack<string>();

			string composedString = ""; // @TODO cleanup

			int max = tokens.Count;
			int i = 0;

			while (i < max)
			{
				Token tok = tokens[i];
				string token = tok.token;



				void AddError(string message)
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = tok,
					});
				}



				if (parsingStateStack.Count > 0)
				{
					ParsingState state = parsingStateStack.Peek();

					switch (state)
					{
						case ParsingState.OutputProjectAssign:
							curProject.output = stringDataStack.Pop();
							parsingStateStack.Pop();
							continue;
						case ParsingState.ComposeString:
							if (token[0] == '#')
							{
								// @TODO cleanup
								if (token == "#projectName")
								{
									composedString += projectName;
								}
								else
								{
									AddError("Can't use this compiler directive when precomposing strings.");
									return;
								}
							}
							else if (token == ".")
							{
								// @TODO cleanup
							}
							else if (token[0] == '\'')
							{
								composedString += token.Substring(1, token.Length - 2);
							}
							else if (token == ";")
							{
								stringDataStack.Push(composedString);
								parsingStateStack.Pop();
							}
							else
							{
								AddError("Can't use this when precomposing strings.");
								return;
							}
							break;

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
										AddError("Compiler directive not implemented in #Compile.");
										return;
									}
								}
								else if (token[0] == '\'')
								{
									curProject.name = token; // @TODO parse string
								}
								else
								{
									AddError("Can't accept this token as #Compile project name.");
									return;
								}
							}
							break;

						case ParsingState.HashCompileBlock:
							switch (token)
							{
								case "entryPoint":
									// @TODO cleanup
									if (tokens[i + 1].token == "=" && tokens[i + 3].token == ";")
									{
										curProject.entryPoint = tokens[i + 2].token;
										i += 3;
									}
									else
									{
										AddError("Only direct symbol assignment to entryPoint is supported.");
										return;
									}
									break;
								case "output":
									// @TODO cleanup
									if (tokens[i + 1].token == "=")
									{
										i++;
										composedString = "";
										parsingStateStack.Push(ParsingState.OutputProjectAssign);
										parsingStateStack.Push(ParsingState.ComposeString);
									}
									else
									{
										AddError("Output can only be assigned.");
										return;
									}
									break;
								case "format":
									// @TODO cleanup
									if (tokens[i + 1].token == "=" && tokens[i + 3].token == ";")
									{
										if (tokens[i + 2].token == "Flat")
										{
											curProject.format = Project.Format.Flat;
											i += 3;
										}
										else
										{
											AddError("Unsupported project output format. Only Flat is supported.");
											return;
										}
									}
									else
									{
										AddError("Only direct symbol assignment to entryPoint is supported.");
										return;
									}
									break;
								default:
									AddError("Unsupported token in #Compile block.");
									return;
							}
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

				i++;
			}
		}
	}
}
