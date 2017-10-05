using System;
using System.Collections.Generic;
using System.Linq;
using static Foxlang.FoxlangCompiler;

namespace Foxlang
{
	internal partial class CompileUnit
	{
		delegate bool DirectiveFn();

		// shared with CompileUnit:
		List<OutputMessage> outputMessages;
		List<UnitInfo> units;

		List<Function> functions;
		List<Var> consts;
		List<Var> vars;
		UnitInfo curUnit;
		Project curProject;
		// /

		List<Token> tokens = new List<Token>();

		Stack<ParsingState> parsingStateStack = new Stack<ParsingState>();
		Stack<string> stringDataStack = new Stack<string>();
		Stack<Block> blockStack = new Stack<Block>();
		Stack<string> namespaceStack = new Stack<string>();

		Function curFunction;

		int iMax;
		int i;

		Token tok;
		string token;

		string filePath;


		Dictionary<string, DirectiveFn> directiveDict;

		internal CompileUnit(FoxlangCompiler fc)
		{
			outputMessages = fc.outputMessages;
			units = fc.units;
			functions = fc.functions;
			consts = fc.consts;
			vars = fc.vars;
			curUnit = fc.curUnit;
			curProject = fc.curProject;

			directiveDict = new Dictionary<string, DirectiveFn>()
			{
				{ "#Put4", () => {

					Require("(");

					i++;
					curFunction.byteCode.Add(ByteCode.Put4BytesHere);

					uint ii;

					if (tokens[i].token == "#address") // @TODO cleanup
					{
						curFunction.byteCode.Add((ByteCode)curUnit.relativeAddress);
					}
					else if (ParseLiteral(tokens[i].token, out ii)) {
						curFunction.byteCode.Add((ByteCode)ii);
					}
					else if (tokens[i].token == "addressOf") // @TODO @hack
					{
						i++;
						AddLabelReference(true);
					}
					else
					{
						// @TODO cleanup
						curFunction.urVarsUnresolved.Add(new UnresolvedReference
						{
							symbol = tokens[i].token,
							pos = curFunction.byteCode.Count,
							token = tok,
							filename = filePath,
						});

						curFunction.byteCode.Add(ByteCode.VarFeedMe);
					}

					Require(")");
					Require(";");

					return true;
				} },

				{ "#Align", () => {

					Require("(");

					uint val;

					i++;
					if (ParseLiteral(tokens[i].token, out val) == false)
						return AddError("#Align needs a numeric literal.");

					curFunction.byteCode.Add(ByteCode.Align);
					curFunction.byteCode.Add((ByteCode)val);

					Require(")");
					Require(";");

					return true;

				} },

				{ "#format", () => {

					Require("=");

					UnitInfo.Format format;

					i++;
					if (Enum.TryParse(tokens[i].token, out format) == false)
						return AddError("Invalid #format: " + tokens[i].token);
					
					curUnit.format = format;
						
					Require(";");

					return true;
				} },

				{ "#Compile", () => {

					curUnit = new UnitInfo
					{
						files = new List<string>(),
						run = new List<string>(),
					};
					units.Add(curUnit);

					parsingStateStack.Push(ParsingState.HashCompile);

					return true;

				} },

				{ "#address", () => {

					uint ii;

					Require("=");
										
					i++;
					if (ParseLiteral(tokens[i].token, out ii) == false)
						return AddError("Expected a literal. Can't parse as a literal: " + tokens[i].token);

					Require(";");

					curUnit.relativeAddress = ii;
					
					return true;

				} },

				{ "#extension", () => {

					if (Require("=") == false)
						return AddError("#extension = '.foo';"); // @TODO
						
					i++;
					if (StringLiteralTryParse(tokens[i].token, out curUnit.extension) == false)
						return AddError("Invalid string literal: " + tokens[i].token);

					if (Require(";") == false)
						return AddError("#extension = '.foo';"); // @TODO

					return true;

				} },
			};
		}

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
			else if (tokens[i + 2].token == "=")
			{
				i += 3;
				string strVal = tokens[i].token;
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

						MathEl curEl = new MathEl();

						if (ConstMathParse(curEl) == false)
							return false;
						// AddError("Can't parse this literal of type '" + type.ToString() + "'."); // @TODO

						value = curEl.val;

						break;
					case FoxlangType.String:
						value = strVal.Substring(1, strVal.Length - 2);
						break;
					default:
						AddError("Can't parse literal of type '" + type.ToString() + "'."); // @TODO
						return false;
				}

				outVar.value = value;

				i--; // @TODO why does this happen?
				if (Require(";") == false)
					return AddError("Missing ;"); // @TODO

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

		bool Require(string requiredToken)
		{
			i++;
			if (tokens[i].token != requiredToken)
			{
				throw new InvalidSyntaxException("Expected '" + requiredToken + "', got '" + tokens[i].token + "'.", tokens[i], filePath);
			}
			else
				return true;
		}

		public bool Compile(string filePath)
		{
			this.filePath = filePath;

			LexerParse(filePath, tokens);

			curFunction = null; // @TODO cleanup?
			string composedString = ""; // @TODO cleanup

			iMax = tokens.Count;
			i = 0;

			if (curUnit == null)
				curUnit = new UnitInfo();

			while (i < iMax)
			{
				tok = tokens[i];
				token = tok.token;

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
											return AddError("Not implemented."); // @TODO
																				 /*curFunction.byteCode.Add(ByteCode.PopRW); // @TODO don't pop if argument used more than once
																				 curFunction.byteCode.Add(ByteCode.Ebx);
																				 //curFunction.byteCode.Add(ByteCode.MovRMemRB);
																				 curFunction.byteCode.Add(ByteCode.Eax);
																				 curFunction.byteCode.Add(ByteCode.Ebx);*/
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
											return AddError("Not implemented."); // @TODO
																				 //curFunction.byteCode.Add(ByteCode.MovRMemImmB);
																				 /*curFunction.byteCode.Add(ByteCode.Eax);
																				 curFunction.byteCode.Add((ByteCode)foundVar.value);*/
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

										i += 2;
										AddLabelReference();

										i += 2;
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

									curFunction.byteCode.Add(ByteCode.RetNear);

									i += 1;

									break;
								default:
									FoxlangType type;
									if (ExitingBlock())
									{
										if (curFunction.byteCode.Last() != ByteCode.RetNear) // @TODO possible conflicts with literal value of .RetNear?
											curFunction.byteCode.Add(ByteCode.RetNear);
										parsingStateStack.Pop();
									}
									else if (token[0] == '#')
									{
										if (directiveDict.ContainsKey(token) == false)
											return AddError("Unknown compiler directive.");

										if (directiveDict[token]() == false)
											return false;
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
											curFunction.byteCode.Add(ByteCode.StringLiteralFeedMe);
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

														curFunction.byteCode.Add(ByteCode.IntImmB);
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
											curFunction.byteCode.Add(ByteCode.StringLiteralFeedMe);
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
								curUnit.run.Add(stringDataStack.Pop());
								parsingStateStack.Pop();
							}
							else
							{
								AddError("Expected ';'."); // @TODO
								return false;
							}
							break;
						case ParsingState.HashCompileRunBlock:
							switch (token)
							{
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
							curUnit.files.Add(stringDataStack.Pop());
							parsingStateStack.Pop();
							if (token != ";")
							{
								AddError("Expected ';'."); // @TODO
								return false;
							}
							break;
						case ParsingState.OutputProjectAssign:
							curUnit.output = stringDataStack.Pop();
							parsingStateStack.Pop();
							continue;
						case ParsingState.ComposeString:
							if (token[0] == '#')
							{
								// @TODO cleanup
								if (token == "#projectName")
								{
									composedString += curProject.name;
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
										composedString += curUnit.output;
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
										curUnit.name = curProject.name;
									else
									{
										AddError("Compiler directive not implemented in #Compile.");
										return false;
									}
								}
								else if (token[0] == '\'')
								{
									curUnit.name = token; // @TODO parse string
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
										curUnit.entryPoint = tokens[i + 2].token;
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
											curUnit.format = UnitInfo.Format.Flat;
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
					if (token[0] == '#')
					{
						if (directiveDict.ContainsKey(token) == false)
							return AddError("Unknown compiler directive.");

						if (directiveDict[token]() == false)
							return false;
					}
					else switch (token)
					{
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

			return true;
		}

		void AddLabelReference(bool isAbsolute = false)
		{
			curFunction.urLabelsUnresolved.Add(new UnresolvedReference()
			{
				symbol = tokens[i].token,
				pos = curFunction.byteCode.Count,
				token = tokens[i],
				filename = filePath,
				isAbsolute = isAbsolute,
			});

			curFunction.byteCode.Add(ByteCode.LabelFeedMe);
		}
	}
}
