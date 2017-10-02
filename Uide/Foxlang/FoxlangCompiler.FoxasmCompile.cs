using System;
using System.Collections.Generic;
using System.IO;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
		class MovSide
		{
			internal enum SideType { Invalid, Register, MemoryAccess, ImmediateValue, StringLiteral, VariableReference }

			internal int width = 0;
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

				void AddWarning(string message)
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Warning,
						message = message,
						token = tok,
						filename = filePath,
					});
				}

				bool ParseSide(out MovSide side)
				{
					side = new MovSide();

					if (tokens[i].token == "[")
					{
						i++;
						side.type = MovSide.SideType.MemoryAccess;
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
						if (side.type == MovSide.SideType.Invalid)
							side.type = MovSide.SideType.Register;
					}
					else if (ParseLiteral(tokens[i].token, out immediate))
					{
						side.byteCode = (ByteCode)immediate;
						if (side.type == MovSide.SideType.Invalid)
							side.type = MovSide.SideType.ImmediateValue;
					}
					else
					{
						side.stringValue = tokens[i].token;
						if (side.type == MovSide.SideType.Invalid)
							side.type = MovSide.SideType.VariableReference;
					}

					if (side.type == MovSide.SideType.MemoryAccess)
					{
						i++;
						if (tokens[i].token != "]")
						{
							return false;
						}
					}

					return true;
				}


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
				else if (token == "#format")
				{
					Project.Format format;

					if (Enum.TryParse(tokens[i + 1].token, out format))
					{
						AddWarning("#format isn't implemented – is always Flat. Ignoring for now.");
						i += 1;
					}
					else
						return AddError("Invalid #format.");

				}
				else if (token == "#address")
				{
					if (ParseLiteral(tokens[i + 1].token, out relativeAddress))
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
						case ByteCode.Hlt:
						case ByteCode.Cli:
							curFunction.byteCode.Add(inByte);
							break;
						case ByteCode.Int:
							{
								uint ii;
								if (ParseLiteral(tokens[i + 1].token, out ii))
								{
									curFunction.byteCode.Add(inByte);
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

						case ByteCode.Mov:
							width = 0;
							goto MovCommon;

						MovCommon:
							{
								MovSide left, right;

								i++;
								if (ParseSide(out left) == false)
									return AddError("Can't parse left side."); // @TODO better errors?

								if (left.type == MovSide.SideType.StringLiteral)
									return AddError("Can't Mov to string literal.");
								// @TODO variable references on left side


								i++;
								if (tokens[i].token != "=" && tokens[i].token != ",")
									return AddError("Mov foo = bar."); // @TODO better errors?


								i++;
								if (ParseSide(out right) == false)
									return AddError("Can't parse right side."); // @TODO better errors?

								if (left.type == MovSide.SideType.MemoryAccess && right.type == MovSide.SideType.MemoryAccess)
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
								
								
								if (left.type == MovSide.SideType.MemoryAccess)
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

										curFunction.byteCode.Add(ByteCode.RRMem);

										// swap sides:
										MovSide temp = left;
										left = right;
										right = temp;
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
									}
								}
								else if (right.type == MovSide.SideType.MemoryAccess)
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
											return AddError("No register size detected. This shouldn't happen! (#2)"); // @TODO cleanup numbers

										curFunction.byteCode.Add(ByteCode.RRMem);
									}
									else
									{
										return AddError("Not implemented.");
										// @TODO This is probably wrong!
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRmImmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRmImmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRmImmB);
										else
											return AddError("Indeterminant operand size on left side. (#1)"); // @TODO better error message

										curFunction.byteCode.Add(ByteCode.RRMem);
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
									}
								}
								curFunction.byteCode.Add(left.byteCode);
								curFunction.byteCode.Add(right.byteCode);


								break;
							}
						case ByteCode.Push:
							{
								string t = tokens[i + 1].token;
								string literal;
								if (StringLiteralTryParse(t, out literal))
								{
									if (curFunction.bits == Bits.Bits16)
										curFunction.byteCode.Add(ByteCode.PushW);
									else
										curFunction.byteCode.Add(ByteCode.PushL);
									curFunction.byteCode.Add(ByteCode.StringLiteralFeedMe);
									curFunction.literalReferences.Add(new SymbolReference
									{
										pos = curFunction.byteCode.Count - 1,
										symbol = literal,
									});
									i += 1;
								}
								else
								{
									AddError("Can't parse string literal.");
									return false;
								}
								break;
							}
						case ByteCode.Call:
							curFunction.byteCode.Add(ByteCode.Call);
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
						case ByteCode.Je:
						case ByteCode.Jne:
							{
								curFunction.byteCode.Add(inByte);
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


				void AddError(string message) // @TODO @cleanup
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = tok,
						filename = filePath,
					});
				}


				if (tokens[i + 1].token == "=")
				{
					uint ii;
					if (ParseLiteral(tokens[i + 2].token, out ii) == false)
					{
						AddError("Can't parse this literal."); // @TODO
						return false;
					}

					vars.Add(new Var
					{
						symbol = token,
						value = ii
					});
				}
				else
				{
					AddError("Foo = bar"); // @TODO
					return false;
				}

				i += 3;
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

				Var foundVar = null;
				foreach (var sym in vars)
				{
					if (sym.symbol == r.symbol)
					{
						foundVar = sym;
						break;
					}
				}

				if (foundVar == null)
					return AddError("Data definition not found.");

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
