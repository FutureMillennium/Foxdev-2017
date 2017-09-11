using System;
using System.Collections.Generic;
using System.IO;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
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
						case ByteCode.Mov:
							{
								ByteCode left, right;
								bool isLeftMem = false,
									isRightR = false;
								int width, rWidth;

								string t = tokens[i + 1].token;
								if (tokens[i + 1].token == "[" && tokens[i + 3].token == "]") // @TODO
								{
									t = tokens[i + 2].token;
									isLeftMem = true;

									i += 2;
								}

								if (RegisterTryParse(t, out left, out width) == false)
								{
									AddError("Unknown register.");
									return false;
								}

								if (tokens[i + 2].token != "=")
								{
									AddError("Mov foo = bar."); // @TODO
									return false;
								}


								UInt32 ii;
								string literal;

								if (StringLiteralTryParse(tokens[i + 3].token, out literal))
								{
									right = (ByteCode)0xFEED1133;
									curFunction.literalReferences.Add(new SymbolReference
									{
										pos = curFunction.byteCode.Count + 2,
										symbol = literal,
									});
								}
								else if (RegisterTryParse(tokens[i + 3].token, out right, out rWidth))
								{
									isRightR = true;
								}
								else if (ParseLiteral(tokens[i + 3].token, out ii))
								{

									right = (ByteCode)ii;
								}
								else
								{

									tok = tokens[i + 3];
									token = tok.token;

									right = (ByteCode)0xFEED11E5;

									curFunction.urVarsUnresolved.Add(new UnresolvedReference
									{
										symbol = token,
										pos = curFunction.byteCode.Count + 2,
										token = tok,
										filename = filePath,
									});
								}

								i += 3;

								if (isLeftMem)
								{
									if (isRightR)
									{
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRMemRL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRMemRW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRMemRB);
									}
									else
									{
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRMemImmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRMemImmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRMemImmB);
									}
								}
								else
								{
									if (isRightR)
									{
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRRL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRRW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRRB);
									}
									else
									{
										if (width == 4)
											curFunction.byteCode.Add(ByteCode.MovRImmL);
										else if (width == 2)
											curFunction.byteCode.Add(ByteCode.MovRImmW);
										else if (width == 1)
											curFunction.byteCode.Add(ByteCode.MovRImmB);
									}
								}
								curFunction.byteCode.Add(left);
								curFunction.byteCode.Add(right);


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
									curFunction.byteCode.Add((ByteCode)0xFEED1133);
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
							curFunction.byteCode.Add((ByteCode)0xFEED11E1);

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
								int width = 4;

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
								int width;

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
								curFunction.byteCode.Add((ByteCode)0xFEED11E1);

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
								int width;
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
