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
		enum LexingState { Normal, IgnoringUntilNewLine, ReadingString, ReadingDoubleString, NestedComments }
		enum ParsingState { HashCompile, HashCompileBlock, ComposeString, OutputProjectAssign, AddFileProject, HashCompileRunBlock, AddRunFileProject, Const, FunctionBlock }
		enum FoxlangType { Byte4, Address4, String, Uint }
		enum Block { Namespace }
		enum ByteCode : UInt32 { Cli, MovEspIm, PushL }

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

		class Var
		{
			public string symbol;
			public FoxlangType type;
		}

		class SymbolReference
		{
			public int pos;
			public string symbol;
		}

		class Function
		{
			public string symbol;
			public List<ByteCode> byteCode = new List<ByteCode>();
			public List<SymbolReference> unresolvedReferences = new List<SymbolReference>();
			public List<SymbolReference> literalReferences = new List<SymbolReference>();
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
		List<Var> vars = new List<Var>();
		List<Function> functions = new List<Function>();
		List<string> stringLiterals = new List<string>();
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
										readingState = LexingState.NestedComments;
									}
									else goto BreakingSymbol;
									break;
								case '"':
									readingState = LexingState.ReadingDoubleString;
									goto BreakingSymbol;
								case '\'':
									readingState = LexingState.ReadingString;
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

			Function curFunction = null; // @TODO cleanup?
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

				bool AcceptNamespace()
				{
					if (tokens[i + 1].token == "{")
					{
						i++;
						namespaceStack.Push(token);
						blockStack.Push(Block.Namespace);
						return true;
					}
					else
					{
						return false;
					}
				}

				bool ExitingBlock()
				{
					if (token == "}")
					{
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

						return true;
					}
					else
					{
						return false;
					}
				}



				if (parsingStateStack.Count > 0)
				{
					ParsingState state = parsingStateStack.Peek();

					switch (state)
					{
						case ParsingState.FunctionBlock:
							switch (token)
							{
								case "Cli":
									if (tokens[i + 1].token == "(" && tokens[i + 2].token == ")" && tokens[i + 3].token == ";")
									{
										curFunction.byteCode.Add(ByteCode.Cli);
										i += 3;
									}
									else
									{
										AddError("Cli();"); // @TODO
										return false;
									}
									break;
								case "%esp":
									if (tokens[i + 1].token == "=" && tokens[i + 3].token == ";")
									{
										/*int j = i + 3;
										string symbol = tokens[i + 2].token;
										while (tokens[j].token != ";")
										{
											if (tokens[j].token == ".")
											{
												symbol += tokens[j + 1].token;
												j += 2;
											}
											else
											{
												AddError("Not allowed."); // @TODO
												return false;
											}
										}
										i = j;*/
										curFunction.byteCode.Add(ByteCode.MovEspIm);
										bool found = false;
										foreach (Const c in consts)
										{
											if (c.symbol == tokens[i + 2].token)
											{
												curFunction.byteCode.Add((ByteCode)c.value);
												found = true;
												break;
											}
										}
										if (found == false)
										{
											AddError("Undefined const symbol."); // @TODO
											return false;
										}
										i += 3;
									}
									break;
								default:
									if (tokens[i + 1].token == "(" && tokens[i + 3].token == ")" && tokens[i + 4].token == ";")
									{
										string arg1 = tokens[i + 2].token;
										if (arg1[0] == '"' && arg1.Last() == '"')
										{
											string literal = arg1.Substring(1, arg1.Length - 2);
											curFunction.byteCode.Add(ByteCode.PushL);
											curFunction.byteCode.Add((ByteCode)0xFEED1133);
											curFunction.literalReferences.Add(new SymbolReference
											{
												pos = curFunction.byteCode.Count - 1,
												symbol = literal,
											});
											i += 4;
										}
										else
										{
											AddError("Can't parse this argument."); // @TODO
											return false;
										}
									}
									else
									{
										AddError("Can't parse this."); // @TODO
										return false;
									}
									break;
							}
							break;

						case ParsingState.Const:

							FoxlangType type;
							if (Enum.TryParse(token, out type))
							{
								// @TODO cleanup
								if (tokens[i + 2].token == "=" && tokens[i + 4].token == ";")
								{
									string strVal = tokens[i + 3].token;
									dynamic value = null;

									switch (type)
									{
										case FoxlangType.Address4:
										case FoxlangType.Uint:
											UInt32 multiplier = 1;
											if (strVal.Last() == 'M')
											{
												strVal = strVal.Substring(0, strVal.Length - 1);
												multiplier = 1024 * 1024;
											}

											UInt32 ii;
											if (UInt32.TryParse(strVal, out ii))
											{
												value = ii * multiplier;
											}
											else
											{
												AddError("Can't parse this literal of type '" + type.ToString() + "'."); // @TODO
												return false;
											}

											break;
										case FoxlangType.String:
											value = strVal.Substring(1, strVal.Length - 2);
											break;
										default:
											AddError("Can't parse literal of type '" + type.ToString() + "'."); // @TODO
											return false;
									}

									consts.Add(new Const {
										symbol = string.Join(".", namespaceStack) + "." + tokens[i + 1].token,
										type = type,
										value = value, // @TODO parse literals etc.
									});
									i += 4;
								}
								else
								{
									AddError("Extra tokens after " + type.ToString() + "."); // @TODO
									return false;
								}
								break;
							}
							else if (ExitingBlock())
							{
								if (blockStack.Count == 0)
								{
									parsingStateStack.Pop();
								}
							}
							else
							{
								if (AcceptNamespace() == false)
								{
									AddError("Can't parse this at this place."); // @TODO
									return false;
								}
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
							AddError("Unknown parser state."); // @TODO
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
						case "function":
							// @TODO cleanup
							if (tokens[i + 2].token == "(" && tokens[i + 3].token == ")" && tokens[i + 4].token == "{")
							{
								curFunction = new Function
								{
									symbol = tokens[i + 1].token,
								};
								functions.Add(curFunction);
								i += 4;
								parsingStateStack.Push(ParsingState.FunctionBlock);
							}
							else
							{
								AddError("function foo() { }."); // @TODO
								return false;
							}
							break;
						default:
							if (AcceptNamespace())
							{

							}
							else if (ExitingBlock())
							{

							}
							else
							{
								FoxlangType type;
								if (Enum.TryParse(token, out type))
								{
									// @TODO cleanup
									if (tokens[i + 2].token == ";")
									{
										vars.Add(new Var
										{
											symbol = string.Join(".", namespaceStack) + "." + tokens[i + 1].token,
											type = type,
										});
										i += 2;
									}
									else
									{
										AddError("Extra tokens after " + type.ToString() + "."); // @TODO
										return false;
									}
									break;
								}
								else
								{
									AddError("Unknown token."); // @TODO
									return false;
								}
							}
							break;
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
