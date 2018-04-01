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
		internal UnitInfo curUnit;
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
		Register[] registers = // general purpose registers
		{
			new Register
			{
				registerBC = ByteCode.Eax
			},
			new Register
			{
				registerBC = ByteCode.Ecx
			},
			new Register
			{
				registerBC = ByteCode.Edx
			},
			new Register
			{
				registerBC = ByteCode.Ebx
			},
			new Register
			{
				registerBC = ByteCode.Esi
			},
			new Register
			{
				registerBC = ByteCode.Edi
			},
		};

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
				{ "#Put2", () => { // @TODO cleanup

					Require("(");

					i++;
					curFunction.byteCode.Add(ByteCode.Put2BytesHere);

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

				{ "#Put4", () => { // @TODO cleanup

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

				{ "#bits", () => {

					Require("=");

					i++;
					if (ParseLiteral(tokens[i].token, out uint bitNum))
					{
						if (bitNum == 32)
						{
							curFunction.bits = Bits.Bits32;
						}
						else if (bitNum == 16)
						{
							curFunction.bits = Bits.Bits16;
						}
						else
						{
							return AddError("Unsupported #bits value.");
						}
					}
					else
					{
						AddError("Can't parse this literal."); // @TODO
						return false;
					}

					Require(";");

					return true;
				} },

				{ "#WriteData", () => {
					Require("(");
					Require(")");
					Require(";");
					curFunction.byteCode.Add(ByteCode.WriteDataHere);
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
						if (curFunction.byteCode.Last() != ByteCode.RetNear) // @TODO possible conflicts with literal value of .RetNear?
							curFunction.byteCode.Add(ByteCode.RetNear);
						break;
					case Block.While:
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

		bool ParseVar(out Var outVar, bool isAddNamespace = true)
		{
			FoxlangType type;
			if (Enum.TryParse(token, out type) == false)
			{
				outVar = null;
				return false;
			}

			outVar = new Var()
			{
				type = type,
			};

			// @TODO cleanup
			if (tokens[i + 1].token == "Pointer")
			{
				outVar.pointerType = type;
				outVar.type = FoxlangType.Pointer;
				i += 1;
				type = FoxlangType.Pointer;
			}

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
				string strVal;

				MathEl curEl = new MathEl();

				if (StringLiteralTryParse(tokens[i].token, out strVal))
					outVar.value = strVal;
				else if (ConstMathParse(curEl))
				{
					outVar.value = curEl.val;
					i--; // @TODO why does this happen?
				}
				else
					return false;

				// @TODO type checking
				/*switch (type)
				{
					case FoxlangType.Byte:
					case FoxlangType.Char:
					case FoxlangType.Byte4:
					case FoxlangType.Address4:
					case FoxlangType.Index:
					case FoxlangType.UInt:

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

				outVar.value = value;*/
				
				Require(";");

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
			Stack<ValueEl> valueStack = new Stack<ValueEl>(); // @TODO cleanup
			ValueEl curElValue = null; // @TODO cleanup
			SymbolReference curLoopEnd = null; // @TODO cleanup
			SymbolReference curLoopStart = null; // @TODO cleanup

			void StringLiteralOut(string literal)
			{
				curFunction.byteCode.Add(ByteCode.StringLiteralFeedMe);
				curFunction.literalReferences.Add(new SymbolReference
				{
					pos = curFunction.byteCode.Count - 1,
					symbol = literal,
				});
			}


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
								case "(":
									// @TODO
									break;
								case ")":
									// @TODO resolve nested ( )'s

									parsingStateStack.Pop();

									break;
								case "[":
									// @TODO check stack validity

									if (curElValue.op != Operation.None)
									{
										curElValue = new ValueEl();
										valueStack.Push(curElValue);
									}

									curElValue.isMemoryAccess = true;
									break;
								case "]":
									// @TODO check curElValue
									if (curElValue.isMemoryAccess == false)
										return AddError("Got closing ']' with no preceding memory access '['.");

									// OK

									//parsingStateStack.Pop(); // @TODO?
									break;
								case ";":
									parsingStateStack.Pop();
									i -= 1;
									break;
								case "+":
									return AddError("Not implemented: '+'.");
									/*if (tokens[i + 1].token == "1")
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
									break;*/
								/*case "valueof":

									break;*/
								case "!=":
									// @TODO check curElValue
									curElValue.op = Operation.NotEqual;

									// @TODO cleanup?
									curElValue = new ValueEl();
									valueStack.Push(curElValue);

									break;
								case "=":
									// @TODO check curElValue
									curElValue.op = Operation.Assignment;

									// @TODO cleanup?
									curElValue = new ValueEl();
									valueStack.Push(curElValue);

									break;
								case "++":
									// @TODO check curElValue
									curElValue.op = Operation.Add1;

									break;
								default:
									{
										Var foundVar;
										string nToken = MakeNamespace(token);

										if (token[0] == '%')
										{
											ByteCode register;
											int width;
											if (RegisterTryParse(token, out register, out width) == false)
												return AddError("Can't parse this register."); // @TODO

											curElValue.type = typeof(RegisterToken);
											curElValue.val = new RegisterToken
											{
												registerBC = register,
												width = width,
											};

										}
										else if (StringLiteralTryParse(token, out string strVal))
										{
											curElValue.type = typeof(string);
											curElValue.val = strVal;
										}
										else if (FindVar(token, out foundVar, curFunction.arguments))
										{
											return AddError("Not implemented."); // @TODO
											/*curFunction.byteCode.Add(ByteCode.PopRW); // @TODO don't pop if argument used more than once
											curFunction.byteCode.Add(ByteCode.Ebx);
											//curFunction.byteCode.Add(ByteCode.MovRMemRB);
											curFunction.byteCode.Add(ByteCode.Eax);
											curFunction.byteCode.Add(ByteCode.Ebx);*/
										}
										else if (FindVar(token, out foundVar, curFunction.localVars))
										{
											curElValue.type = typeof(Var);
											curElValue.val = foundVar;
										}
										else if (FindVar(nToken, out foundVar, vars))
										{
											return AddError("Not implemented."); // @TODO
											/*curFunction.byteCode.Add(ByteCode.AddRMem);
											curFunction.byteCode.Add(ByteCode.Eax);
											curFunction.byteCode.Add((ByteCode)0xFEED1135);
											curFunction.varReferences.Add(new VarReference
											{
												pos = curFunction.byteCode.Count - 1,
												var = foundVar,
											});*/
										}
										else if (FindVar(nToken, out foundVar, consts))
										{
											curElValue.isConst = true;
											curElValue.type = typeof(Var);
											curElValue.val = foundVar;
										}
										else if (ParseLiteral(token, out uint newVal))
										{
											// @TODO check curElValue
											curElValue.type = typeof(uint);
											curElValue.val = newVal;
										}
										else
										{
											AddError("Undeclared symbol: " + token); // @TODO
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
								Var newVar;
								if (ParseVar(out newVar, false))
								{
									curFunction.arguments.Add(newVar);
									i--;
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
								case "Cli": // @TODO cleanup
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
										curFunction.byteCode.Add(ByteCode.JmpRelB);

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
									blockStack.Push(Block.While);
									parsingStateStack.Push(ParsingState.Condition);

									curElValue = new ValueEl();
									valueStack.Push(curElValue);
									parsingStateStack.Push(ParsingState.ValueParsing);

									break;
								case "return":
									if (tokens[i + 1].token != ";") // @TODO
										return AddError("Returning values isn't implemented.");

									curFunction.byteCode.Add(ByteCode.RetNear);

									i += 1;

									break;
								default:
									if (ExitingBlock())
									{
										parsingStateStack.Pop();
									}
									else if (token[0] == '#')
									{
										if (directiveDict.ContainsKey(token) == false)
											return AddError("Unknown compiler directive.");

										if (directiveDict[token]() == false)
											return false;
									}
									/*else if (token[0] == '%' && tokens[i + 1].token == "=") // @TODO cleanup
									{
										/*
										i += 2;
										string literal;
										MathEl mathEl = new MathEl();

										if (StringLiteralTryParse(tokens[i].token, out literal))
										{
											curFunction.byteCode.Add(ByteCode.StringLiteralFeedMe);
											curFunction.literalReferences.Add(new SymbolReference
											{
												pos = curFunction.byteCode.Count - 1,
												symbol = literal,
											});
										}
										else if (ConstMathParse(mathEl))
										{
											curFunction.byteCode.Add((ByteCode)mathEl.val);
										}
										else
											return false;

										i--;
										Require(";");*/
									/*}*/
									/*else if (tokens[i + 1].token == "[")
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
									}*/
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
									/*else if (tokens[i + 1].token == "+=" && tokens[i + 3].token == ";")
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
									}*/
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
											StringLiteralOut(literal);

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
									else if (ParseVar(out Var var, false))
									{
										curFunction.localVars.Add(var);

										Register reg = FindFreeRegister();
										// @TODO all registers are taken

										var.register = reg;
										reg.var = var;
										curFunction.byteCode.Add(ByteCode.MovRImmL); // @TODO size
										curFunction.byteCode.Add(reg.registerBC);
										if (var.type == FoxlangType.Pointer && var.pointerType == FoxlangType.Char)
										{
											StringLiteralOut(var.value);
										}
										else
										{
											// @TODO other types
											curFunction.byteCode.Add((ByteCode)var.value);
										}
									}
									else
									{
										parsingStateStack.Push(ParsingState.Assignment);

										// @TODO cleanup: should be in a function?

										curElValue = new ValueEl();
										valueStack.Push(curElValue);

										parsingStateStack.Push(ParsingState.ValueParsing);
										i -= 1;
									}
									break;
							}
							break;

						case ParsingState.Const:

							{
								Var newVar;
								if (ParseVar(out newVar))
								{
									if (blockStack.Count == 0 || blockStack.Peek() != Block.Namespace)
									{
										parsingStateStack.Pop();
									}

									consts.Add(newVar);
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

						case ParsingState.Assignment:
							{
								ValueEl leftEl = valueStack.Pop();
								ValueEl rightEl = null;

								if (valueStack.Count > 0)
								{
									rightEl = leftEl;
									leftEl = valueStack.Pop();
								}

								if (leftEl.op == Operation.Assignment)
								{

									if (leftEl.type == typeof(RegisterToken))
									{
										var left = (RegisterToken)leftEl.val;

										if (rightEl.isConst)
										{
											int width = left.width;

											if (width == 4)
												curFunction.byteCode.Add(ByteCode.MovRImmL);
											else if (width == 2)
												curFunction.byteCode.Add(ByteCode.MovRImmW);
											else if (width == 1)
												curFunction.byteCode.Add(ByteCode.MovRImmB);
											else
												return AddError("Unknown register width. This is likely a compiler bug, as this shouldn't happen.");

											curFunction.byteCode.Add(left.registerBC);

											curFunction.byteCode.Add((ByteCode)((Var)rightEl.val).value);
										}
										else
										{
											if (rightEl.type == typeof(RegisterToken))
											{
												var right = (RegisterToken)rightEl.val;
												int width = left.width;

												if (rightEl.isMemoryAccess)
												{
													// @TODO cleanup:
													if (width == 4)
														curFunction.byteCode.Add(ByteCode.MovRRmL);
													else if (width == 2)
														curFunction.byteCode.Add(ByteCode.MovRRmW);
													else if (width == 1)
														curFunction.byteCode.Add(ByteCode.MovRRmB);
													else
														return AddError("Unknown register width. This is likely a compiler bug, as this shouldn't happen."); // @TODO cleanup numbers

													curFunction.byteCode.Add(ByteCode.RRMem);

													// 8B /r	MOV r32,r/m32	RM	Valid	Valid	Move r/m32 to r32.
													curFunction.byteCode.Add(left.registerBC);
													curFunction.byteCode.Add(right.registerBC);
												}
												else
												{
													if (width != right.width)
														return AddError("Register sizes don't match. Can't assign.");

													// @TODO cleanup:
													if (width == 4)
														curFunction.byteCode.Add(ByteCode.MovRmRL);
													else if (width == 2)
														curFunction.byteCode.Add(ByteCode.MovRmRW);
													else if (width == 1)
														curFunction.byteCode.Add(ByteCode.MovRmRB);
													else
														return AddError("Unknown register width. This is likely a compiler bug, as this shouldn't happen."); // @TODO cleanup numbers

													curFunction.byteCode.Add(ByteCode.RToR);

													// 89 mov order: to, from
													curFunction.byteCode.Add(left.registerBC);
													curFunction.byteCode.Add(right.registerBC);
												}
											}
											else if (rightEl.type == typeof(string))
											{
												int width = left.width;

												// @TODO cleanup:
												if (width == 4)
													curFunction.byteCode.Add(ByteCode.MovRImmL);
												else if (width == 2)
													curFunction.byteCode.Add(ByteCode.MovRImmW);
												else if (width == 1)
													curFunction.byteCode.Add(ByteCode.MovRImmB);
												else
													return AddError("Unknown register width. This is likely a compiler bug, as this shouldn't happen.");

												curFunction.byteCode.Add(left.registerBC);
												StringLiteralOut((string)rightEl.val);
											}
											else if (rightEl.type == typeof(uint))
											{
												int width = left.width;

												// @TODO cleanup:
												if (width == 4)
													curFunction.byteCode.Add(ByteCode.MovRImmL);
												else if (width == 2)
													curFunction.byteCode.Add(ByteCode.MovRImmW);
												else if (width == 1)
													curFunction.byteCode.Add(ByteCode.MovRImmB);
												else
													return AddError("Unknown register width. This is likely a compiler bug, as this shouldn't happen.");

												curFunction.byteCode.Add(left.registerBC);
												curFunction.byteCode.Add((ByteCode)(uint)rightEl.val);
											}
											else
											{
												return AddError("Right side type not implemented: " + rightEl.type);
											}
										}
									}
									else if (leftEl.type == typeof(Var))
									{
										var left = (Var)leftEl.val;

										if (rightEl.type == typeof(Var))
										{
											var right = (Var)rightEl.val;

											Register reg = FindFreeRegister();
											// @TODO all registers are taken

											reg.var = right; // @TODO flag memory access

											curFunction.byteCode.Add(ByteCode.MovRRmB); // @TODO size
											curFunction.byteCode.Add(ByteCode.RRMem);
											curFunction.byteCode.Add(reg.registerBC);
											curFunction.byteCode.Add(right.register.registerBC);

											curFunction.byteCode.Add(ByteCode.MovRmRB); // @TODO size
											curFunction.byteCode.Add(ByteCode.RRMem);
											curFunction.byteCode.Add(reg.registerBC);
											curFunction.byteCode.Add(left.register.registerBC);
										}
										else
										{
											var right = (uint)rightEl.val;
											curFunction.byteCode.Add(ByteCode.MovRmImmB); // @TODO size
											curFunction.byteCode.Add(ByteCode.RRMem);
											curFunction.byteCode.Add(left.register.registerBC);
											curFunction.byteCode.Add((ByteCode)right);
										}
									}
									else
									{
										return AddError("Assignment not implemented.");
									}
								}
								else if (leftEl.op == Operation.Add1)
								{
									curFunction.byteCode.Add(ByteCode.IncR);
									if (leftEl.type == typeof(RegisterToken))
										curFunction.byteCode.Add(((RegisterToken)leftEl.val).registerBC);
									else if (leftEl.type == typeof(Var))
										curFunction.byteCode.Add(((Var)leftEl.val).register.registerBC);
									else
										return AddError("Type not implemented: " + leftEl.type);
								}
								else
								{
									return AddError("Operation not implemented.");
								}

								parsingStateStack.Pop();
							}
							break;

						case ParsingState.Condition:
							{
								// @TODO check valueStack

								ValueEl leftEl = valueStack.ElementAt(1);
								ValueEl rightEl = valueStack.ElementAt(0);

								curLoopStart = new SymbolReference
								{
									pos = curFunction.byteCode.Count,
								};
								curFunction.labels.Add(curLoopStart);

								if (leftEl.isMemoryAccess) // @TODO other ops
								{
									curFunction.byteCode.Add(ByteCode.CmpRMemImmB); // @TODO other sizes

									// @TODO cleanup

									if (leftEl.type == typeof(Var))
									{
										curFunction.byteCode.Add(((Var)leftEl.val).register.registerBC);
									}
									else if (leftEl.type == typeof(RegisterToken))
									{
										curFunction.byteCode.Add(((RegisterToken)leftEl.val).registerBC);
									}
									else
									{
										return AddError("Left side type not implemented: " + leftEl.type);
									}

									curFunction.byteCode.Add((ByteCode)((uint)rightEl.val)); // @TODO other types
								}
								else
								{
									return AddError("Not implemented: non-memory access condition.");
								}

								if (leftEl.op == Operation.NotEqual)
								{
									curFunction.byteCode.Add(ByteCode.JeRelB); // @TODO will be different for non-"while" loop

									curLoopEnd = new SymbolReference();
									curFunction.labels.Add(curLoopEnd);

									curFunction.urLabelsUnresolved.Add(new UnresolvedReference()
									{
										pos = curFunction.byteCode.Count,
										filename = filePath,
										isAbsolute = false,
										reference = curLoopEnd,
									});

									curFunction.byteCode.Add(ByteCode.LabelFeedMe);

								}
								else
								{
									return AddError("Not implemented: Non-!= operation.");
								}

								valueStack.Clear();

								parsingStateStack.Pop();
								parsingStateStack.Push(ParsingState.FunctionBlock);
							}
							break;

						case ParsingState.While: // at the end of a while ( ) { } block

							curFunction.byteCode.Add(ByteCode.JmpRelB);

							curFunction.urLabelsUnresolved.Add(new UnresolvedReference()
							{
								pos = curFunction.byteCode.Count,
								filename = filePath,
								isAbsolute = false,
								reference = curLoopStart,
							});

							curFunction.byteCode.Add(ByteCode.LabelFeedMe);

							curLoopEnd.pos = curFunction.byteCode.Count;
							parsingStateStack.Pop();
							i -= 1;

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
								Var newVar;
								if (ParseVar(out newVar))
								{
									vars.Add(newVar);
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

		Register FindFreeRegister()
		{
			Register reg = null;
			for (var j = 0; j < registers.Length; j++)
			{
				if (registers[j].var == null)
				{
					reg = registers[j];
					break;
				}
			}

			return reg;
		}
	}
}
