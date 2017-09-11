using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
		enum LexingState { Normal, IgnoringUntilNewLine, ReadingString, ReadingDoubleString, NestedComments }
		enum ParsingState { HashCompile, HashCompileBlock, ComposeString, OutputProjectAssign, AddFileProject, HashCompileRunBlock, AddRunFileProject, Const, FunctionBlock, FunctionArguments, ValueParsing, ArrayAccess, While, Condition }
		enum FoxlangType { Byte, Uint8, Char, Int8, Byte2, Uint16, Int16, Byte4, Address4, Index, Uint, Uint32, Pointer, Int, Int32, String }
		enum Block { Namespace, Function }
		enum Bits { Bits16, Bits32 }

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
			public long bytePos;
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
			public long bytePos;
			public int bytes;
			public string symbol;
			public string filename;
			public Token token;
			public SymbolReference reference;
		}

		class Function
		{
			public string symbol;
			public Bits bits = Bits.Bits32;
			public List<Var> arguments = new List<Var>();
			public List<ByteCode> byteCode = new List<ByteCode>();
			public List<UnresolvedReference> unresolvedReferences = new List<UnresolvedReference>();
			public List<UnresolvedReference> urLabelsUnresolved = new List<UnresolvedReference>();
			public List<UnresolvedReference> urVarsUnresolved = new List<UnresolvedReference>();
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
		Function entryPoint;
		uint relativeAddress = 0;
		public string output;

		public bool CompileProject(string filePath)
		{


			void GlobalErrorMessage(string message)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Error,
					message = message,
					filename = filePath,
				});
			}

			/*void GlobalWarningMessage(string message)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Warning,
					message = message,
					filename = filePath,
				});
			}*/


			projectName = Path.GetFileNameWithoutExtension(filePath);
			
			if (Compile(filePath) == false)
			{
				return false;
			}

			/*if (projects.Count == 0)
			{
				GlobalErrorMessage("No projects inside project file. This is probably bad.");
				return false;
			}*/

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

			if (functions.Count == 0)
			{
				GlobalErrorMessage("No functions found. Nothing to compile.");
				return false;
			}

			if (projects.Count == 0)
			{
				entryPoint = null;
				foreach (Function func in functions)
				{
					if (func.symbol == "EntryPoint")
					{
						entryPoint = func;
						break;
					}
				}
				if (entryPoint == null)
				{
					GlobalErrorMessage("No projects found and no function EntryPoint().");
					return false;
				}
				// @TODO
			}

			// @TODO compile
			//GlobalWarningMessage("Binary compilation not implemented yet. Not outputting anything.");

			string outputFile = Path.ChangeExtension(filePath, ".com"); // @TODO non-.com binaries
			output = outputFile;

			if (BytecodeCompileToBinary(outputFile) == false)
				return false; // @TODO error

			return true;
		}

		bool ParseLiteral(string strVal, out UInt32 ii)
		{
			UInt32 multiplier = 1;
			System.Globalization.NumberStyles baseNum = System.Globalization.NumberStyles.Integer;

			if (strVal.Length > 2)
				if (strVal.StartsWith("0x"))
				{
					strVal = strVal.Substring(2);
					baseNum = System.Globalization.NumberStyles.HexNumber;
				}
			/*else if (strVal.StartsWith("0b"))
			{
				strVal = strVal.Substring(2);
				baseNum = System.Globalization.NumberStyles.HexNumber;
			}*/ // @TODO binary and octal literals UGH C# WHY

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

		public bool Compile(string filePath)
		{
			List<Token> tokens = new List<Token>();

			LexerParse(filePath, tokens);

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



				bool AddError(string message)
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = tok,
						filename = filePath,
					});
					return false;
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
							case FoxlangType.Char:
							case FoxlangType.Byte4:
							case FoxlangType.Address4:
							case FoxlangType.Index:
							case FoxlangType.Uint:
							case FoxlangType.Pointer:
								UInt32 ii;
								//if (strVal == "_" && )
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
										curFunction.byteCode.Add(ByteCode.IncR);
										curFunction.byteCode.Add(ByteCode.Eax);
										i += 1;
									}
									else
									{
										AddError("Trying to add unsupported type or literal.");
										return false;
									}
									break;
								/*case "valueof":

									break;*/
								default:
									{
										Var foundVar;
										string nToken = MakeNamespace(token);
										if (FindVar(token, out foundVar, curFunction.arguments))
										{
											curFunction.byteCode.Add(ByteCode.PopRW); // @TODO don't pop if argument used more than once
											curFunction.byteCode.Add(ByteCode.Ebx);
											curFunction.byteCode.Add(ByteCode.MovRMemRB);
											curFunction.byteCode.Add(ByteCode.Eax);
											curFunction.byteCode.Add(ByteCode.Ebx);
										}
										else if (FindVar(nToken, out foundVar, vars))
										{
											curFunction.byteCode.Add(ByteCode.AddRMem);
											curFunction.byteCode.Add(ByteCode.Eax);
											curFunction.byteCode.Add((ByteCode)0xFEED1135);
											curFunction.varReferences.Add(new VarReference
											{
												pos = curFunction.byteCode.Count - 1,
												var = foundVar,
											});
										}
										else if (FindVar(nToken, out foundVar, consts))
										{
											curFunction.byteCode.Add(ByteCode.MovRMemImmB);
											curFunction.byteCode.Add(ByteCode.Eax);
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
							if (token == ")")
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
									AddError("Missing function block start ('{')."); // @TODO
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
										i--;
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
								/*case "%esp":
									if (tokens[i + 1].token == "=" && tokens[i + 3].token == ";")
									{
										curFunction.byteCode.Add(ByteCode.MovRImmL);
										curFunction.byteCode.Add(ByteCode.Esp);
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
									break;*/
								case "while":
									parsingStateStack.Push(ParsingState.While);
									parsingStateStack.Push(ParsingState.Condition);
									parsingStateStack.Push(ParsingState.ValueParsing);
									break;
								case "return":
									if (tokens[i + 1].token != ";") // @TODO
										return AddError("Returning values isn't implemented.");

									curFunction.byteCode.Add(ByteCode.Ret);

									i += 1;

									break;
								default:
									FoxlangType type;
									if (ExitingBlock())
									{
										if (curFunction.byteCode.Last() != ByteCode.Ret) // @TODO possible conflicts with literal value of .Ret?
											curFunction.byteCode.Add(ByteCode.Ret);
										parsingStateStack.Pop();
									}
									else if (token[0] == '%' && tokens[i + 1].token == "=" && tokens[i + 3].token == ";")
									{
										ByteCode register;
										int width;
										if (RegisterTryParse(token, out register, out width) == false)
											return AddError("Can't parse this register."); // @TODO

										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRImmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRImmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRImmB);
										else
											return AddError("Unknown register width!"); // @TODO

										curFunction.byteCode.Add(register);

										string t = tokens[i + 2].token;
										uint ii;
										string literal;

										if (StringLiteralTryParse(t, out literal))
										{
											curFunction.byteCode.Add((ByteCode)0xFEED1133);
											curFunction.literalReferences.Add(new SymbolReference
											{
												pos = curFunction.byteCode.Count - 1,
												symbol = literal,
											});
										}
										else if (ParseLiteral(tokens[i + 2].token, out ii))
										{
											curFunction.byteCode.Add((ByteCode)ii);
										}
										else
										{
											return AddError("Register assignment only accepts literals."); // @TODO
										}

										i += 3;
									}
									else if (tokens[i + 1].token == "[")
									{
										Var foundConst;
										if (FindVar(MakeNamespace(token), out foundConst, consts))
										{
											curFunction.byteCode.Add(ByteCode.MovRImmL);
											curFunction.byteCode.Add(ByteCode.Eax);
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
									/*else if (tokens[i + 1].token == "=")
									{
										Var foundVar;
										string nToken = MakeNamespace(token);
										// @TODO local variables
										if (FindVar(nToken, out foundVar, vars))
										{
											// @TODO if a variable is declared later?
										}
										else
										{
											AddError("Undeclared variable.");
											return false;
										}

										// @TODO

										curFunction.byteCode.Add((ByteCode)0xFEED1135);
										curFunction.varReferences.Add(new VarReference
										{
											pos = curFunction.byteCode.Count - 1,
											var = foundVar,
										});

										// @TODO
									}*/
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
									else if (tokens[i + 1].token == "(" && tokens[i + 3].token == ")" && tokens[i + 4].token == ";") // foo(bar);
									{
										ByteCode instruction;
										string arg1 = tokens[i + 2].token;

										if (Enum.TryParse(token, out instruction))
										{
											switch (instruction)
											{
												case ByteCode.Int:
													{
														uint ii;
														if (ParseLiteral(arg1, out ii) == false)
															return AddError("Only accepts a literal. Can't parse this as a literal.");

														curFunction.byteCode.Add(instruction);
														curFunction.byteCode.Add((ByteCode)ii);

														i += 4; // @TODO @cleanup

														break;
													}
												default:
													return AddError("Instruction not implemented.");
											}
										}
										else if (arg1[0] == '"' && arg1.Last() == '"')
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
									else if (token[0] == '.' && tokens[i + 1].token == ":") // .FooLabel:
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
											curFunction.byteCode.Add(ByteCode.PopRL);
											curFunction.byteCode.Add(ByteCode.Ecx);
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
						case "#address":
							{
								uint ii;
								if (ParseLiteral(tokens[i + 1].token, out ii) == false)
									return AddError("Expected a literal. Can't parse this as a literal.");

								relativeAddress = ii;

								i += 1;

								break;
							}
						case "const":
							parsingStateStack.Push(ParsingState.Const);
							break;
						case "function":
							if (tokens[i + 2].token == "(") // @TODO
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
								// do nothing more
							}
							else if (ExitingBlock())
							{
								// do nothing more
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

		bool StringLiteralTryParse(string literal, out string outS)
		{
			outS = null;

			if ((literal[0] == '"' && literal.Last() == '"') || (literal[0] == '\'' && literal.Last() == '\''))
			{
				outS = literal.Substring(1, literal.Length - 2);
				return true;
			}

			return false;
		}

		bool RegisterTryParse(string token, out ByteCode register, out int width)
		{
			if (token.Length >= 2)
			{
				token = (char)(token[1] - ('a' - 'A')) + token.Substring(2);
			}

			width = 0;

			if (Enum.TryParse(token, out register) == false)
				return false;

			switch (register)
			{
				case ByteCode.Al:
				case ByteCode.Cl:
				case ByteCode.Dl:
				case ByteCode.Bl:
				case ByteCode.Ah:
				case ByteCode.Ch:
				case ByteCode.Dh:
				case ByteCode.Bh:
					width = 1;
					break;
				case ByteCode.Ax:
				case ByteCode.Cx:
				case ByteCode.Dx:
				case ByteCode.Bx:
					width = 2;
					break;
				case ByteCode.Eax:
				case ByteCode.Ecx:
				case ByteCode.Edx:
				case ByteCode.Ebx:
				case ByteCode.Esp:
				case ByteCode.Ebp:
				case ByteCode.Esi:
				case ByteCode.Edi:
					width = 4;
					break;
				default:
					return false;
			}

			return true;
		}

		byte RegisterNumber(ByteCode register)
		{
			switch(register)
			{
				case ByteCode.Al:
				case ByteCode.Ax:
				case ByteCode.Eax:
					return 0;
				case ByteCode.Cl:
				case ByteCode.Cx:
				case ByteCode.Ecx:
					return 1;
				case ByteCode.Dl:
				case ByteCode.Dx:
				case ByteCode.Edx:
					return 2;
				case ByteCode.Bl:
				case ByteCode.Bx:
				case ByteCode.Ebx:
					return 3;
				case ByteCode.Sp:
				case ByteCode.Esp:
				case ByteCode.Ah:
					return 4;
				case ByteCode.Bp:
				case ByteCode.Ebp:
				case ByteCode.Ch:
					return 5;
				case ByteCode.Si:
				case ByteCode.Esi:
				case ByteCode.Dh:
					return 6;
				case ByteCode.Di:
				case ByteCode.Edi:
				case ByteCode.Bh:
					return 6;
			}
			return 0xFF;
		}

		public string EchoBytecode()
		{
			Function f = entryPoint;

			if (f == null)
				return "";

			StringBuilder sb = new StringBuilder();

			int iMax = f.byteCode.Count;
			int i = 0;
			int iLit = 0,
				iURLabels = 0,
				iURVars = 0;
			int untilLine = 0;

			while (i < iMax)
			{
				ByteCode b = f.byteCode[i];

				switch (b)
				{
					case ByteCode.RRMemOffset1:
					case ByteCode.RRMemOffset4:
						untilLine += 1;
						goto default;

					case ByteCode.PushL:
					case ByteCode.Call:
					case ByteCode.Jmp:
					case ByteCode.Je:
					case ByteCode.Jne:
					case ByteCode.PopRW:
					case ByteCode.PopRL:
					case ByteCode.IncR:
					case ByteCode.Int:
						untilLine = 1;
						goto default;
					case ByteCode.MovRImmL:
					case ByteCode.MovRImmW:
					case ByteCode.MovRImmB:
					case ByteCode.MovRMemImmL:
					case ByteCode.MovRMemImmW:
					case ByteCode.MovRMemImmB:
					case ByteCode.CmpRMemImmB:
						untilLine = 2;
						goto default;
					case ByteCode.MovRRmB:
					case ByteCode.MovRRmW:
					case ByteCode.MovRRmL:
						untilLine = 3;
						goto default;
					case (ByteCode)0xFEED11E1: // label
											   //sb.AppendLine(b.ToString("x"));
						sb.Append(f.urLabelsUnresolved[iURLabels].symbol);
						iURLabels++;
						break;
					case (ByteCode)0xFEED1133: // literal
						sb.Append("\"" + f.literalReferences[iLit].symbol + "\"");
						iLit++;
						break;
					case (ByteCode)0xFEED11E5: // .data var
						sb.Append(f.urVarsUnresolved[iURVars].symbol);
						iURVars++;
						//sb.AppendLine(b.ToString("x"));
						break;
					default:
						sb.Append(b.ToString());
						break;
				}

				if (untilLine == 0)
					sb.AppendLine();
				else
				{
					//sb.Append('(' + b.ToString("x") + ")");
					sb.Append(' ');
					untilLine--;
				}

				i++;
			}

			return sb.ToString();
		}
	}
}
