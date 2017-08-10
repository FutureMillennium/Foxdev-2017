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
		enum ParsingState { HashCompile, HashCompileBlock, ComposeString, OutputProjectAssign, AddFileProject, HashCompileRunBlock, AddRunFileProject, Const, FunctionBlock, FunctionArguments, ValueParsing, ArrayAccess }
		enum FoxlangType { Byte, Char, Byte4, Address4, Index, Uint, Pointer, String }
		enum Block { Namespace, Function }
		enum ByteCode : UInt32 { Cli, Hlt, MovEspImm, MovEaxIm, AddEaxMem, IncEax, PopBx, PopEcx, MovBMemEaxBl, MovBMemEaxImm, AddLMemImm, PushL, Jmp, Call, Ret }

		public class Token
		{
			public string token;
			public int line,
				col,
				pos;
		}

		class Var
		{
			public string symbol;
			public FoxlangType type;
			public dynamic value;
		}

		class SymbolReference
		{
			public int pos;
			public string symbol;
		}

		class VarReference
		{
			public int pos;
			public Var var;
		}

		class UnresolvedReference
		{
			public int pos;
			public string symbol;
			public string filename;
			public Token token;
		}

		class Function
		{
			public string symbol;
			public List<Var> arguments = new List<Var>();
			public List<ByteCode> byteCode = new List<ByteCode>();
			public List<UnresolvedReference> unresolvedReferences = new List<UnresolvedReference>();
			public List<VarReference> varReferences = new List<VarReference>();
			public List<SymbolReference> literalReferences = new List<SymbolReference>();
			public List<SymbolReference> labels = new List<SymbolReference>();
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
		List<Var> consts = new List<Var>();
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

			foreach (Function f in functions)
			{
				foreach (UnresolvedReference r in f.unresolvedReferences)
				{
					bool resolved = false;
					foreach (Function ff in functions)
					{
						if (ff.symbol == r.symbol)
						{
							resolved = true;
							break;
						}
					}
					if (resolved == false)
					{
						outputMessages.Add(new OutputMessage
						{
							type = OutputMessage.MessageType.Error,
							message = "Unresolved symbol: " + r.symbol,
							token = r.token,
							filename = r.filename,
						});
						return false;
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
								case ';':
								case '(':
								case ')':
								case '[':
								case ']':
								case ':':
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
			#endregion
			
			#region Parsing

			Stack<ParsingState> parsingStateStack = new Stack<ParsingState>();
			Stack<string> stringDataStack = new Stack<string>();
			Stack<Block> blockStack = new Stack<Block>();
			Stack<string> namespaceStack = new Stack<string>();

			Function curFunction = null; // @TODO cleanup?
			string composedString = ""; // @TODO cleanup

			int iMax = tokens.Count;
			int i = 0;

			while (i < iMax)
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
							case Block.Function:
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

				bool ParseLiteral(string strVal, out UInt32 ii)
				{
					UInt32 multiplier = 1;
					System.Globalization.NumberStyles baseNum = System.Globalization.NumberStyles.Integer;

					if (strVal.Length > 2 && strVal.StartsWith("0x"))
					{
						strVal = strVal.Substring(2);
						baseNum = System.Globalization.NumberStyles.HexNumber;
					}

					if (strVal.Last() == 'M')
					{
						strVal = strVal.Substring(0, strVal.Length - 1);
						multiplier = 1024 * 1024;
					}
					
					if (UInt32.TryParse(strVal, baseNum,
System.Globalization.CultureInfo.CurrentCulture, out ii))
					{
						ii *= multiplier;
						return true;
					}
					else
					{
						return false;
					}
				}

				bool ParseVar(FoxlangType type, out Var outVar, bool isAddNamespace = true)
				{
					// @TODO cleanup
					if (tokens[i + 1].token == "Pointer")
					{
						type = FoxlangType.Pointer;
						i += 1;
						// @TODO type of Pointer
					}

					outVar = new Var
					{
						type = type,
					};
					if (isAddNamespace)
						outVar.symbol = MakeNamespace(tokens[i + 1].token);
					else
						outVar.symbol = tokens[i + 1].token;

					if (tokens[i + 2].token == ";" || tokens[i + 2].token == ")")
					{
						i += 2;
						return true;
					}
					else if (tokens[i + 2].token == "=" && tokens[i + 4].token == ";")
					{
						string strVal = tokens[i + 3].token;
						dynamic value = null;

						switch (type)
						{
							case FoxlangType.Byte:
							case FoxlangType.Byte4:
							case FoxlangType.Address4:
							case FoxlangType.Index:
							case FoxlangType.Uint:
							case FoxlangType.Pointer:
								UInt32 ii;
								if (ParseLiteral(strVal, out ii))
								{
									value = ii;
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

						outVar.value = value;
						i += 4;

						return true;
					}
					else
					{
						AddError("Extra tokens after " + type.ToString() + "."); // @TODO
						return false;
					}
					//break;
				}

				bool FindVar(string symbol, out Var foundVar, List<Var> lookInList)
				{
					foundVar = null;
					foreach (Var v in lookInList)
					{
						if (v.symbol == symbol)
						{
							foundVar = v;
							return true;
						}
					}

					return false;
				}

				string MakeNamespace(string name)
				{
					if (namespaceStack.Count > 0)
						return string.Join(".", namespaceStack.Reverse()) + "." + name;
					else
						return name;
				}


				if (parsingStateStack.Count > 0)
				{
					ParsingState state = parsingStateStack.Peek();

					switch (state)
					{
						case ParsingState.ArrayAccess:
							if (token == "=")
							{
								parsingStateStack.Push(ParsingState.ValueParsing);
							}
							else if (token == ";")
							{
								parsingStateStack.Pop();
							}
							else
							{
								AddError("Unexpected token in array access.");
								return false;
							}
							break;

						case ParsingState.ValueParsing:
							switch (token)
							{
								case "]":
									parsingStateStack.Pop();
									break;
								case ";":
									parsingStateStack.Pop();
									i -= 1;
									break;
								case "+":
									if (tokens[i + 1].token == "1")
									{
										curFunction.byteCode.Add(ByteCode.IncEax);
										i += 1;
									}
									else
									{
										AddError("Trying to add unsupported type or literal.");
										return false;
									}
									break;
								default:
									{
										Var foundVar;
										string nToken = MakeNamespace(token);
										if (FindVar(token, out foundVar, curFunction.arguments))
										{
											curFunction.byteCode.Add(ByteCode.PopBx); // @TODO don't pop if argument used more than once
											curFunction.byteCode.Add(ByteCode.MovBMemEaxBl);
										}
										else if (FindVar(nToken, out foundVar, vars))
										{
											curFunction.byteCode.Add(ByteCode.AddEaxMem);
											curFunction.byteCode.Add((ByteCode)0xFEED1135);
											curFunction.varReferences.Add(new VarReference
											{
												pos = curFunction.byteCode.Count - 1,
												var = foundVar,
											});
										}
										else if (FindVar(nToken, out foundVar, consts))
										{
											curFunction.byteCode.Add(ByteCode.MovBMemEaxImm);
											curFunction.byteCode.Add((ByteCode)foundVar.value);
										}
										else
										{
											AddError("Undeclared symbol, I guess?"); // @TODO
											return false;
										}
										break;
									}
							}
							break;

						case ParsingState.FunctionArguments:
							if (token == "{")
							{
								// @TODO cleanup
								parsingStateStack.Pop();
								blockStack.Push(Block.Function);
								parsingStateStack.Push(ParsingState.FunctionBlock);
							}
							else if (token == ")")
							{
								if (tokens[i + 1].token == "{")
								{
									// @TODO cleanup
									parsingStateStack.Pop();
									blockStack.Push(Block.Function);
									parsingStateStack.Push(ParsingState.FunctionBlock);
									i += 1;
								}
								else
								{
									AddError("Missing function block start ('{').");
									return false;
								}
							}
							else
							{
								FoxlangType type;
								if (Enum.TryParse(token, out type))
								{
									Var newVar;
									if (ParseVar(type, out newVar, false))
									{
										curFunction.arguments.Add(newVar);
									}
									else
									{
										return false;
									}
								}
								else
								{
									AddError("Unknown argument type."); // @TODO
									return false;
								}
							}
							break;

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
								case "Hlt":
									if (tokens[i + 1].token == "(" && tokens[i + 2].token == ")" && tokens[i + 3].token == ";")
									{
										curFunction.byteCode.Add(ByteCode.Hlt);
										i += 3;
									}
									else
									{
										AddError("Hlt();"); // @TODO
										return false;
									}
									break;
								case "Jmp":
									if (tokens[i + 1].token == "(" && tokens[i + 3].token == ")" && tokens[i + 4].token == ";")
									{
										curFunction.byteCode.Add(ByteCode.Jmp);
										int jmax = curFunction.labels.Count;
										int found = -1;
										for (int j = 0; j < jmax; j++)
										{
											if (curFunction.labels[j].symbol == tokens[i + 2].token)
											{
												found = j;
												break;
											}
										}
										if (found == -1)
										{
											AddError("Label " + tokens[i + 2].token + " not found"); // @TODO
											return false;
										}
										curFunction.byteCode.Add((ByteCode)(curFunction.labels[found].pos - curFunction.byteCode.Count));
										i += 4;
									}
									else
									{
										AddError("Jmp(.FooLabel);"); // @TODO
										return false;
									}
									break;
								case "%esp":
									if (tokens[i + 1].token == "=" && tokens[i + 3].token == ";")
									{
										curFunction.byteCode.Add(ByteCode.MovEspImm);
										bool found = false;
										foreach (Var c in consts)
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
									FoxlangType type;
									if (ExitingBlock())
									{
										curFunction.byteCode.Add(ByteCode.Ret);
										parsingStateStack.Pop();
									}
									else if (tokens[i + 1].token == "[")
									{
										Var foundConst;
										if (FindVar(MakeNamespace(token), out foundConst, consts))
										{
											curFunction.byteCode.Add(ByteCode.MovEaxIm);
											curFunction.byteCode.Add((ByteCode)foundConst.value);
										}
										else
										{
											// @TODO non-const
											AddError("Undeclared constant.");
											return false;
										}

										parsingStateStack.Push(ParsingState.ArrayAccess);
										parsingStateStack.Push(ParsingState.ValueParsing);
										i += 1;
									}
									else if (tokens[i + 1].token == "+=" && tokens[i + 3].token == ";")
									{
										Var foundVar;
										string nToken = MakeNamespace(token);
										if (FindVar(nToken, out foundVar, vars))
										{

										}
										else
										{
											AddError("Undeclared variable.");
											return false;
										}

										if (tokens[i + 2].token[0] >= '2' && tokens[i + 2].token[0] <= '9')
										{
											curFunction.byteCode.Add(ByteCode.AddLMemImm);
											curFunction.byteCode.Add((ByteCode)0xFEED1135);
											curFunction.varReferences.Add(new VarReference
											{
												pos = curFunction.byteCode.Count - 1,
												var = foundVar,
											});

											UInt32 ii;
											if (ParseLiteral(tokens[i + 2].token, out ii))
											{
												curFunction.byteCode.Add((ByteCode)ii);
												i += 3;
											}
											else
											{
												AddError("Can't parse this literal.");
												return false;
											}
										}
										else
										{
											AddError("Trying to add unsupported symbol or literal to variable.");
											return false;
										}
									}
									else if (tokens[i + 1].token == "(" && tokens[i + 3].token == ")" && tokens[i + 4].token == ";")
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

											curFunction.byteCode.Add(ByteCode.Call);
											curFunction.byteCode.Add((ByteCode)0xFEED113F);

											curFunction.unresolvedReferences.Add(new UnresolvedReference
											{
												symbol = token,
												pos = curFunction.byteCode.Count - 1,
												token = tok,
												filename = filePath,
											});
											i += 4;
										}
										else
										{
											AddError("Can't parse this argument."); // @TODO
											return false;
										}
									}
									else if (token[0] == '.' && tokens[i + 1].token == ":")
									{
										curFunction.labels.Add(new SymbolReference
										{
											pos = curFunction.byteCode.Count,
											symbol = token,
										});
										i += 1;
									}
									else if (Enum.TryParse(token, out type))
									{
										// @TODO hack af
										if (tokens[i + 1].token == "Pointer"
											&& tokens[i + 3].token == "="
											&& tokens[i + 5].token == ";")
										{
											curFunction.byteCode.Add(ByteCode.PopEcx);
											i += 5;
										}
										else
										{
											AddError("Unsupported local variable declaration."); // @TODO
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

							{
								FoxlangType type;
								if (Enum.TryParse(token, out type))
								{
									Var newVar;
									if (ParseVar(type, out newVar))
									{
										if (blockStack.Count == 0 || blockStack.Peek() != Block.Namespace)
										{
											parsingStateStack.Pop();
										}

										consts.Add(newVar);
									}
									else
									{
										return false;
									}
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
										AddError("Can't parse this inside const block."); // @TODO
										return false;
									}
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
							if (tokens[i + 2].token == "(")
							{
								curFunction = new Function
								{
									symbol = MakeNamespace(tokens[i + 1].token),
								};
								functions.Add(curFunction);
								i += 2;

								parsingStateStack.Push(ParsingState.FunctionArguments);
							}
							else
							{
								AddError("function foo() { }."); // @TODO
								return false;
							}
							break;
						case "namespace":
							if (tokens[i + 2].token == ":")
							{
								namespaceStack.Push(tokens[i + 1].token);
								i += 2;
							}
							else
							{
								AddError("namespace foo:"); // @TODO
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
									Var newVar;
									if (ParseVar(type, out newVar))
									{
										vars.Add(newVar);
									}
									else
									{
										return false;
									}
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
