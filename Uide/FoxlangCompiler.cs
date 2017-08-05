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
		enum ReadingState { Normal, IgnoringUntilNewLine, ReadingString, NestedComments }
		enum ParsingState { HashCompile, HashCompileBlock, ComposeString, OutputProjectAssign, AddFileProject, HashCompileRunBlock, AddRunFileProject, Const }
		enum FoxlangType { Address4 }
		enum Block { Namespace }

		public class Token
		{
			public string token;
			public int line,
				col,
				pos;
		}

		class Const
		{
			public string symbol;
			public FoxlangType type;
			public dynamic value;
		}

		public class OutputMessage
		{
			public enum MessageType { Notice, Warning, Error }

			public MessageType type;
			public string message,
				filename;
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

		public List<OutputMessage> outputMessages = new List<OutputMessage>();
		public List<Project> projects = new List<Project>();
		List<Const> consts = new List<Const>();
		public string projectName;
		Project curProject;

		public bool CompileProject(string filePath)
		{
			projectName = Path.GetFileNameWithoutExtension(filePath);
			
			if (Compile(filePath) == false)
			{
				return false;
			}

			if (projects.Count == 0)
			{
				GlobalErrorMessage("No projects inside project file. This is probably bad.");
				return false;
			}

			string projectPath = Path.GetDirectoryName(filePath);

			for (int i = 0; i < projects.Count; i++)
			{
				foreach (string wildFile in projects[i].files)
				{
					//string f = file.Replace('/', '\\');
					string f = wildFile;
					if (f[0] == '/')
						f = f.Substring(1);

					if (f.Contains('*'))
					{
						string path = Path.Combine(projectPath, Path.GetDirectoryName(f));
						string[] files = Directory.GetFiles(path, Path.GetFileName(wildFile));

						foreach (string file in files)
						{
							if (Compile(file) == false)
								return false;
						}
					}
					else
					{
						GlobalErrorMessage("Non-wildcard file names not implemented yet.");
					}
				}
			}


			return true;
		}

		public bool Compile(string filePath)
		{
			List<Token> tokens = new List<Token>();

			#region Lexical parsing
			try
			{
				StreamReader streamReader = File.OpenText(filePath);

				char c, prevC = (char)0;
				int pos = 0, line = 1, col = 1;
				Token currentToken = null;
				ReadingState readingState = ReadingState.Normal;
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
					c = (char)streamReader.Read();

					switch (readingState)
					{
						case ReadingState.NestedComments:
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
									readingState = ReadingState.Normal;
								}
								else
								{
									nestedCommentLevel--;
								}
							}

							break;
						case ReadingState.IgnoringUntilNewLine:
							if (c == '\n') // @TODO cleanup
							{
								line++;
								col = 0;
								readingState = ReadingState.Normal;
							}
							break;
						case ReadingState.ReadingString:
							currentToken.token += c;
							if (c == '\n') // @TODO cleanup
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
								case '*':
									if (currentToken != null && currentToken.token == "/")
									{
										currentToken = null;
										nestedCommentLevel = 0;
										readingState = ReadingState.NestedComments;
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
			#endregion
			
			#region Parsing

			Stack<ParsingState> parsingStateStack = new Stack<ParsingState>();
			Stack<string> stringDataStack = new Stack<string>();
			Stack<Block> blockStack = new Stack<Block>();
			Stack<string> namespaceStack = new Stack<string>();

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
						filename = filePath,
					});
				}

				void AddNotice(string message)
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Notice,
						message = message,
						token = tok,
						filename = filePath,
					});
				}



				if (parsingStateStack.Count > 0)
				{
					ParsingState state = parsingStateStack.Peek();

					switch (state)
					{
						case ParsingState.Const:
							switch (token)
							{
								case "}":
									Block exitingBlock = blockStack.Pop();
									switch (exitingBlock)
									{
										case Block.Namespace:
											namespaceStack.Pop();
											break;
										default:
											AddError("Exiting unknown block."); // @TODO
											return false;
									}
									if (blockStack.Count == 0)
									{
										parsingStateStack.Pop();
									}
									break;
								case "Address4":
									// @TODO cleanup
									if (tokens[i + 2].token == "=" && tokens[i + 4].token == ";")
									{
										consts.Add(new Const
										{
											symbol = string.Join(".", namespaceStack) + "." + tokens[i + 1].token,
											type = FoxlangType.Address4,
											value = tokens[i + 2].token, // @TODO parse literals etc.
										});
										i += 4;
									}
									else
									{
										AddError("Extra tokens after Address4."); // @TODO
										return false;
									}
									break;
								default:
									if (tokens[i + 1].token == "{")
									{
										i++;
										namespaceStack.Push(token);
										blockStack.Push(Block.Namespace);
									}
									else
									{
										AddError("Can't parse this at this place."); // @TODO
										return false;
									}
									break;
							}

							break;

						case ParsingState.AddRunFileProject:
							if (token == ";")
							{
								curProject.run.Add(stringDataStack.Pop());
								parsingStateStack.Pop();
							}
							else
							{
								AddError("Expected ';'."); // @TODO
								return false;
							}
							break;
						case ParsingState.HashCompileRunBlock:
							switch (token) {
								case "Requires":
									// @TODO ignoring
									i += 4; // @TODO dangerous
									AddNotice("Ignoring #Compile 'Requires()' for now.");
									break;
								case "Run":
									// @TODO cleanup
									if (tokens[i + 1].token == "(")
									{
										i++;
										composedString = "";
										parsingStateStack.Push(ParsingState.AddRunFileProject);
										parsingStateStack.Push(ParsingState.ComposeString);
									}
									else
									{
										AddError("Run()."); // @TODO
										return false;
									}
									break;
								case "}":
									parsingStateStack.Pop();
									break;
								default:
									AddError("Can't use this in '#Compile run' block."); // @TODO
									return false;
							}
							break;
						case ParsingState.AddFileProject:
							curProject.files.Add(stringDataStack.Pop());
							parsingStateStack.Pop();
							if (token != ";")
							{
								AddError("Expected ';'."); // @TODO
								return false;
							}
							break;
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
									return false;
								}
							}
							else if (token[0] == '\'')
							{
								composedString += token.Substring(1, token.Length - 2);
							}
							else
							{
								switch (token)
								{
									case ".":
											// @TODO cleanup
											break;
									case ";":
									case ")":
										stringDataStack.Push(composedString);
										parsingStateStack.Pop();
											break;
									case "output":
										// @TODO temp
										composedString += curProject.output;
										break;
									default:
										AddError("Can't use this when precomposing strings.");
										return false;
								}
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
										return false;
									}
								}
								else if (token[0] == '\'')
								{
									curProject.name = token; // @TODO parse string
								}
								else
								{
									AddError("Can't accept this token as #Compile project name.");
									return false;
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
										return false;
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
										return false;
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
											return false;
										}
									}
									else
									{
										AddError("Only direct symbol assignment to entryPoint is supported.");
										return false;
									}
									break;
								case "Add":
									if (tokens[i + 1].token == "(")
									{
										i++;
										composedString = "";
										parsingStateStack.Push(ParsingState.AddFileProject);
										parsingStateStack.Push(ParsingState.ComposeString);
									}
									else
									{
										AddError("Add()."); // @TODO
										return false;
									}
									break;
								case "run":
									if (tokens[i + 1].token == "=" && tokens[i + 2].token == "{")
									{
										i += 2;
										parsingStateStack.Push(ParsingState.HashCompileRunBlock);
									}
									else
									{
										AddError("run = { }."); // @TODO
										return false;
									}
									break;
								case "}":
									parsingStateStack.Pop();
									break;
								default:
									AddError("Unsupported token in #Compile block.");
									return false;
							}
							// @TODO
							break;
						default:
							AddError("Unknown state."); // @TODO
							return false;
					}

				}
				else
				{
					switch (token)
					{
						case "#Compile":
							curProject = new Project
							{
								files = new List<string>(),
								run = new List<string>(),
							};
							projects.Add(curProject);

							parsingStateStack.Push(ParsingState.HashCompile);
							break;
						case "const":
							parsingStateStack.Push(ParsingState.Const);
							break;
						default:
							AddError("Unknown token."); // @TODO
							return false;
					}
				}

				i++;
			}
			#endregion

			return true;
		}

		void GlobalErrorMessage(string message)
		{
			System.Windows.MessageBox.Show(message);
		}
	}
}
