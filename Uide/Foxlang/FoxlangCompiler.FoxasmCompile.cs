using System;
using System.Collections.Generic;
using System.IO;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
		class MovSide
		{
			internal enum SideType { Invalid, Register, ImmediateValue, StringLiteral, VariableReference }

			internal int width = 0;
			internal int offset;
			internal bool isMemoryAccess = false;
			internal SideType type = SideType.Invalid;
			internal ByteCode byteCode;
			internal string stringValue;
		}

		public bool FoxasmCompile(string filePath)
		{
			string fileExtension = null;

			List<Token> tokens = new List<Token>();

			LexerParse(filePath, tokens);

			Function curFunction = new Function();
			entryPoint = curFunction;

			int iMax = tokens.Count;
			int i = 0;

			if (curUnit == null)
				curUnit = new UnitInfo();



			bool ParseSide(out MovSide side)
			{
				side = new MovSide();

				if (tokens[i].token == "[")
				{
					i++;
					side.isMemoryAccess = true;
				}
				else if (StringLiteralTryParse(tokens[i].token, out side.stringValue))
				{
					side.byteCode = ByteCode.StringLiteralFeedMe;
					side.type = MovSide.SideType.StringLiteral;
					return true;
				}

				uint immediate;

				if (RegisterTryParse(tokens[i].token, out side.byteCode, out side.width))
				{
					side.type = MovSide.SideType.Register;
				}
				else if (ParseLiteral(tokens[i].token, out immediate))
				{
					side.byteCode = (ByteCode)immediate;
					side.type = MovSide.SideType.ImmediateValue;
				}
				else
				{
					side.stringValue = tokens[i].token;
					side.type = MovSide.SideType.VariableReference;
				}

				if (side.isMemoryAccess)
				{
					i++;

					while (i < iMax)
					{
						switch (tokens[i].token)
						{
							case "+":
								{ // @TODO shouldn't allow offsets for immediate values?
									uint val;
									i++;
									if (ParseLiteral(tokens[i].token, out val))
										side.offset = (int)val;
									else
										return false;
								break;
								}
							case "]":
								return true;
							default:
								return false;
						}

						i++;
					}
				}

				return true;
			}


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

				/*void AddWarning(string message)
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Warning,
						message = message,
						token = tok,
						filename = filePath,
					});
				}*/


				ByteCode inByte;
				if (token[0] == '.' && tokens[i + 1].token == ":") // .FooLabel:
				{
					if (token == ".data")
					{
						i += 2;
						break;
					}

					curFunction.labels.Add(new SymbolReference
					{
						pos = curFunction.byteCode.Count,
						symbol = token,
					});
					i += 1;
				}
				else if (token == "#bits")
				{
					uint bitNum;
					if (ParseLiteral(tokens[i + 1].token, out bitNum))
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
						i += 1;
					}
					else
					{
						AddError("Can't parse this literal."); // @TODO
						return false;
					}
				}
				else if (token == "#Put4")
				{
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
					else if (tokens[i].token[0] == '.')
					{
						curFunction.urLabelsUnresolved.Add(new UnresolvedReference()
						{
							symbol = tokens[i].token,
							pos = curFunction.byteCode.Count,
							token = tok,
							filename = filePath,
							isAbsolute = true,
						});

						curFunction.byteCode.Add(ByteCode.LabelFeedMe);
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

				}
				else if (token == "#Align")
				{
					i++;

					curFunction.byteCode.Add(ByteCode.Align);

					uint ii;

					if (ParseLiteral(tokens[i].token, out ii))
					{
						curFunction.byteCode.Add((ByteCode)ii);
					}
					else
						return AddError("#Align needs a numeric literal.");
				}
				else if (token == "#format")
				{
					UnitInfo.Format format;

					if (Enum.TryParse(tokens[i + 1].token, out format))
					{
						//AddWarning("#format isn't implemented – is always Flat. Ignoring for now."); // @TODO
						i += 1;
					}
					else
						return AddError("Invalid #format.");

				}
				else if (token == "#address")
				{
					if (ParseLiteral(tokens[i + 1].token, out curUnit.relativeAddress))
					{
						i += 1;
					}
					else
					{
						AddError("Can't parse this literal.");
						return false;
					}
				}
				else if (token == "#extension")
				{
					if (StringLiteralTryParse(tokens[i + 1].token, out fileExtension) == false)
						return AddError("Invalid string literal.");

					i += 1;
				}
				else if (Enum.TryParse(token, out inByte))
				{
					int width = 0;

					switch (inByte)
					{
						case ByteCode.Ret:
							curFunction.byteCode.Add(ByteCode.RetNear);
							break;
						case ByteCode.Hlt:
						case ByteCode.Cli:
							curFunction.byteCode.Add(inByte);
							break;
						case ByteCode.Int:
							{
								uint ii;
								if (ParseLiteral(tokens[i + 1].token, out ii))
								{
									curFunction.byteCode.Add(ByteCode.IntImmB);
									curFunction.byteCode.Add((ByteCode)ii);
									i += 1;
								}
								else
								{
									AddError("Can't parse this literal."); // @TODO
									return false;
								}
								break;
							}
						case ByteCode.MovB:
							width = 1;
							goto MovCommon;
						case ByteCode.MovL:
							width = 4;
							goto MovCommon;
						case ByteCode.Mov:
							width = 0;
							goto MovCommon;

						MovCommon:
							{
								MovSide left, right;

								i++;
								if (ParseSide(out left) == false)
									return AddError("Can't parse left side."); // @TODO better errors?

								if (left.isMemoryAccess == false)
								{
									if (left.type == MovSide.SideType.StringLiteral)
										return AddError("Can't Mov to string literal.");
									else if (left.type == MovSide.SideType.ImmediateValue)
										return AddError("Can't Mov to immediate value.");
								}
								// @TODO variable references on left side


								i++;
								if (tokens[i].token != "=" && tokens[i].token != ",")
									return AddError("Mov foo = bar."); // @TODO better errors?


								i++;
								if (ParseSide(out right) == false)
									return AddError("Can't parse right side."); // @TODO better errors?

								if (left.isMemoryAccess && right.isMemoryAccess)
									return AddError("Can't access memory on both sides.");

								if (right.type == MovSide.SideType.StringLiteral)
								{
									curFunction.literalReferences.Add(new SymbolReference
									{
										pos = curFunction.byteCode.Count + 2, // @TODO Might not be true?
										symbol = right.stringValue,
									});
								}
								else if (right.stringValue != null)
								{
									curFunction.urVarsUnresolved.Add(new UnresolvedReference
									{
										symbol = right.stringValue,
										pos = curFunction.byteCode.Count + 2, // @TODO Might not be true?
										token = tok,
										filename = filePath,
									});
								}
								
								
								if (left.isMemoryAccess) // @TODO cleanup
								{
									if (left.type != MovSide.SideType.Register)
									{
										if (right.type == MovSide.SideType.Register)
										{
											if (width == 0)
												width = right.width;

											if (width == 4)
												curFunction.byteCode.Add(ByteCode.MovRmRL);
											else if (width == 2)
												curFunction.byteCode.Add(ByteCode.MovRmRW);
											else if (width == 1)
												curFunction.byteCode.Add(ByteCode.MovRmRB);
											else
												return AddError("No register size detected. This shouldn't happen! (#1)");
											
											curFunction.byteCode.Add(ByteCode.RMemImm);

											// reverse order!
											curFunction.byteCode.Add(right.byteCode);
											curFunction.byteCode.Add(left.byteCode);
										}
										else
											return AddError("Moving immediate value into immediate memory not supported.");
									}
									else if (right.type == MovSide.SideType.Register)
									{
										if (width == 0)
											width = right.width;

										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRmRL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRmRW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRmRB);
										else
											return AddError("No register size detected. This shouldn't happen! (#1)");

										if (left.byteCode == ByteCode.Esp) // @TODO
											return AddError("%Esp memory access isn't supported.");
										else if (left.offset != 0 || left.byteCode == ByteCode.Ebp)
											curFunction.byteCode.Add(ByteCode.RRMemOffset1);
										else
											curFunction.byteCode.Add(ByteCode.RRMem);

										// reverse order!
										curFunction.byteCode.Add(right.byteCode);
										curFunction.byteCode.Add(left.byteCode);

										if (left.offset != 0 || left.byteCode == ByteCode.Ebp)
											curFunction.byteCode.Add((ByteCode)left.offset);
									}
									else
									{
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRmImmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRmImmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRmImmB);
										else
											return AddError("Indeterminant operand size on right side."); // @TODO better error message

										curFunction.byteCode.Add(ByteCode.RRMem);

										curFunction.byteCode.Add(left.byteCode);
										curFunction.byteCode.Add(right.byteCode);
									}
								}
								else if (right.isMemoryAccess)
								{
									if (right.type != MovSide.SideType.Register)
									{
										if (left.type == MovSide.SideType.Register)
										{
											if (width == 0)
												width = left.width;

											if (width == 4)
												curFunction.byteCode.Add(ByteCode.MovRRmL);
											else if (width == 2)
												curFunction.byteCode.Add(ByteCode.MovRRmW);
											else if (width == 1)
												curFunction.byteCode.Add(ByteCode.MovRRmB);
											else
												return AddError("No register size detected. This shouldn't happen! (#1)");

											curFunction.byteCode.Add(ByteCode.RMemImm);

											curFunction.byteCode.Add(left.byteCode);
											curFunction.byteCode.Add(right.byteCode);
										}
										else
											return AddError("Moving immediate value into immediate memory not supported.");
									}
									else if (left.type == MovSide.SideType.Register)
									{
										if (width == 0)
											width = left.width;

										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRRmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRRmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRRmB);
										else
											return AddError("No register size detected. This shouldn't happen! (#2)"); // @TODO cleanup numbers

										if (right.byteCode == ByteCode.Esp) // @TODO
											return AddError("%Esp memory access isn't supported.");
										else if (right.offset != 0 || right.byteCode == ByteCode.Ebp)
											curFunction.byteCode.Add(ByteCode.RRMemOffset1);
										else
											curFunction.byteCode.Add(ByteCode.RRMem);

										curFunction.byteCode.Add(left.byteCode);
										curFunction.byteCode.Add(right.byteCode);

										if (right.offset != 0 || right.byteCode == ByteCode.Ebp)
											curFunction.byteCode.Add((ByteCode)right.offset);
									}
									else
									{
										return AddError("This probably shouldn't happen?");
									}
								}
								else
								{
									if (width == 0)
										width = left.width;

									if (right.type == MovSide.SideType.Register)
									{
										if (width != right.width)
											return AddError("Operand size doesn't match.");

										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRmRL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRmRW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRmRB);
										else
											return AddError("No register size detected. This shouldn't happen! (#3)"); // @TODO cleanup numbers

										curFunction.byteCode.Add(ByteCode.RToR);
										
										// reverse order!
										curFunction.byteCode.Add(right.byteCode);
										curFunction.byteCode.Add(left.byteCode);
									}
									else
									{
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRImmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRImmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRImmB);
										else
											return AddError("Indeterminant operand size on left side. (#2)"); // @TODO better error message

										curFunction.byteCode.Add(left.byteCode);
										curFunction.byteCode.Add(right.byteCode);
									}
								}


								break;
							}
						case ByteCode.PushL:
						case ByteCode.Push:
							{
								string t = tokens[i + 1].token;
								string literal;
								ByteCode register;

								if (StringLiteralTryParse(t, out literal))
								{
									if (curFunction.bits == Bits.Bits16)
										curFunction.byteCode.Add(ByteCode.PushImmW);
									else
										curFunction.byteCode.Add(ByteCode.PushImmL);
									curFunction.byteCode.Add(ByteCode.StringLiteralFeedMe);
									curFunction.literalReferences.Add(new SymbolReference
									{
										pos = curFunction.byteCode.Count - 1,
										symbol = literal,
									});
									i += 1;
								}
								else if (RegisterTryParse(t, out register, out width))
								{
									curFunction.byteCode.Add(ByteCode.PushRL);
									curFunction.byteCode.Add(register);
									i += 1;
								}
								else
									return AddError("Not a string literal or register: " + t);

								break;
							}
						case ByteCode.Call:
							if (curFunction.bits == Bits.Bits16)
								curFunction.byteCode.Add(ByteCode.CallRelW);
							else
								curFunction.byteCode.Add(ByteCode.CallRelL);
							curFunction.byteCode.Add(ByteCode.LabelFeedMe);

							curFunction.urLabelsUnresolved.Add(new UnresolvedReference
							{
								symbol = tokens[i + 1].token,
								pos = curFunction.byteCode.Count - 1,
								token = tokens[i + 1],
								filename = filePath,
							});

							i += 1;
							break;
						case ByteCode.PopW:
						case ByteCode.Pop:
							{
								ByteCode left;
								width = 4;

								if (inByte == ByteCode.PopW)
									width = 2;

								string t = tokens[i + 1].token;
								if (RegisterTryParse(t, out left, out width) == false) // @TODO check width matches
								{
									AddError("Unknown register."); // @TODO
									return false;
								}

								if (width == 2)
									curFunction.byteCode.Add(ByteCode.PopRW);
								else
									curFunction.byteCode.Add(ByteCode.PopRL);
								curFunction.byteCode.Add(left);

								i += 1;

								break;
							}
						case ByteCode.CmpB:
							{
								bool isLeftMem = false;
								ByteCode left;

								string t;
								if (tokens[i + 1].token == "[" && tokens[i + 3].token == "]")
								{
									t = tokens[i + 2].token;
									isLeftMem = true;
									i += 3;
								}
								else
								{
									t = tokens[i + 1].token;
									i += 1;
								}

								if (RegisterTryParse(t, out left, out width) == false)
								{
									AddError("Unknown register."); // @TODO
									return false;
								}

								if (tokens[i + 1].token != ",")
								{
									AddError("CmpB foo, bar."); // @TODO
									return false;
								}

								uint ii;
								if (ParseLiteral(tokens[i + 2].token, out ii))
								{
									if (isLeftMem)
										curFunction.byteCode.Add(ByteCode.CmpRMemImmB);
									else
										curFunction.byteCode.Add(ByteCode.CmpRImmB); // @TODO check register size

									curFunction.byteCode.Add(left);
									curFunction.byteCode.Add((ByteCode)ii);
								}
								else
								{
									AddError("Can't parse this literal."); // @TODO
									return false;
								}

								i += 2;

								break;
							}
						case ByteCode.Jmp:
							curFunction.byteCode.Add(ByteCode.JmpRelB);
							goto JmpCommon;
						case ByteCode.Je:
							curFunction.byteCode.Add(ByteCode.JeRelB);
							goto JmpCommon;
						case ByteCode.Jne:
							curFunction.byteCode.Add(ByteCode.JneRelB);
						JmpCommon:
							{
								curFunction.byteCode.Add(ByteCode.LabelFeedMe);

								token = tokens[i + 1].token;

								curFunction.urLabelsUnresolved.Add(new UnresolvedReference()
								{
									symbol = token,
									pos = curFunction.byteCode.Count - 1,
									token = tok,
									filename = filePath,
								});

								i += 1;
								break;
							}
						case ByteCode.Inc:
							{
								ByteCode left;
								string t = tokens[i + 1].token;

								if (RegisterTryParse(t, out left, out width) == false)
								{
									AddError("Unknown register."); // @TODO
									return false;
								}

								curFunction.byteCode.Add(ByteCode.IncR);
								curFunction.byteCode.Add(left);

								i += 1;
								break;
							}
						default:
							AddError("Instruction not implemented.");
							return false;
					}
				}
				else
				{
					AddError("Unknown instruction.");
					return false;
				}

				i++;
			}

			#region .data segment
			while (i < iMax)
			{
				Token tok = tokens[i];
				string token = tok.token;


				bool AddError(string message) // @TODO @cleanup
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


				if (tokens[i + 1].token == "=")
				{
					i += 2;

					bool canEnd = false;

					Stack<MathEl> stack = new Stack<MathEl>();
					MathEl curEl = new MathEl();
					stack.Push(curEl);

					uint DoOp(uint left, char op, uint right)
					{
						switch (op)
						{
							case '+':
								left += right;
								break;
							case '-':
								left -= right;
								break;
						}
						return left;
					}

					bool DoVar(uint val)
					{
						if (curEl.op != 0 && curEl.val != null)
							curEl.val = DoOp((uint)curEl.val, curEl.op, val);
						else if (curEl.val == null)
							curEl.val = val;
						else
							return AddError("Error: This shouldn't happen.");

						if (stack.Count == 1)
							canEnd = true;

						return true;
					}


					while (i < iMax)
					{
						uint newVal;
						
						if (ParseLiteral(tokens[i].token, out newVal))
						{
							if (DoVar(newVal) == false)
								return false;
						}
						else switch (tokens[i].token)
							{
								case "+":
								case "-":
									curEl.op = tokens[i].token[0];
									canEnd = false;
									break;
								case "(":
									if ((curEl.op == 0 && curEl.val == null)
										|| curEl.val != null)
									{
										curEl = new MathEl();
										stack.Push(curEl);
										canEnd = false;
									}
									else
										return AddError("Unexpected '('.");
									break;
								case ")":
									if (stack.Count > 1)
									{
										stack.Pop();
										MathEl prevEl = stack.Peek();
										if (prevEl.op != 0)
											prevEl.val = DoOp((uint)prevEl.val, prevEl.op, (uint)curEl.val);
										else
											prevEl.val = curEl.val;

										curEl = prevEl;

										if (stack.Count == 1)
											canEnd = true;
									}
									else
										return AddError("Unexpected ')'.");
									break;
								default:
									if (canEnd)
									{
										goto DoEnd;
									}
									else
									{
										Var foundVar = FindVar(tokens[i].token);
										if (foundVar == null)
											return AddError("Can't parse this literal, or undefined constant: " + tokens[i].token);

										if (DoVar((uint)foundVar.value) == false)
											return false;

										canEnd = true;
									}
									break;
							}

						i++;

						continue;
						DoEnd:
							break;
					}

					vars.Add(new Var
					{
						symbol = token,
						value = curEl.val
					});
				}
				else
				{
					AddError("Foo = bar"); // @TODO
					return false;
				}
			}
			#endregion

			foreach (var r in curFunction.urLabelsUnresolved)
			{
				bool AddError(string message) // @TODO @cleanup
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = r.token,
						filename = filePath,
					});
					return false;
				}

				SymbolReference foundSym = null;
				foreach (var sym in curFunction.labels)
				{
					if (sym.symbol == r.symbol)
					{
						foundSym = sym;
						break;
					}
				}

				if (foundSym == null)
					return AddError("Label not found.");

				r.reference = foundSym;
				if (r.isAbsolute)
					curFunction.byteCode[r.pos] = (ByteCode)(foundSym.pos);
				else
					curFunction.byteCode[r.pos] = (ByteCode)(foundSym.pos - (r.pos + 1));
			}

			foreach (var r in curFunction.urVarsUnresolved)
			{
				bool AddError(string message) // @TODO @cleanup
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = r.token,
						filename = filePath,
					});
					return false;
				}

				Var foundVar = FindVar(r.symbol);

				if (foundVar == null)
					return AddError("Undefined: " + r.symbol);

				curFunction.byteCode[r.pos] = (ByteCode)(foundVar.value);
			}

			if (output == null)
			{
				if (fileExtension == null)
					fileExtension = ".bin";

				output = Path.ChangeExtension(filePath, fileExtension);
			}

			if (BytecodeCompileToBinary(output) == false)
				return false; // @TODO error

			return true;
		}
	}
}
