using System;
using System.Collections.Generic;
using System.IO;
using Compilers;
using System.Text;

namespace Foxasm
{
	enum RefType { Label, String, Const };
	enum RefSubType { Relative, Absolute };
	enum Bits { Bits16, Bits32 }
	public enum Format { Invalid, Flat }

	class Label
	{
		public long bytePos;
		public string symbol;
	}

	class FeedMe
	{
		public long bytePos;
		public byte width;
		public Token token;
		public RefType refType;
		public RefSubType subType;
		public StringData refObj;
		public uint offset;
	}

	class StringData
	{
		public string str;
		public long bytePos;
	}

	class Const
	{
		public Token token;
		public int val;
	}

	class Side
	{
		internal Registers reg;
		internal bool isMemoryAccess;
		internal UInt32 offset;
		internal byte width;
		internal FeedMe feedMe;
	}

	partial class Foxasm : Compiler
	{
		internal List<Token> tokens;
		List<Label> labels;
		List<Const> consts = new List<Const>();
		UInt32 address = 0;
		string filePath;
		int iT = 0;
		BinaryWriter writer = null;
		Bits bits = Bits.Bits32;

		static internal bool TryIntLiteralParse(string strVal, out UInt32 ii)
		{
			if (strVal == null)
			{
				ii = 0;
				return false;
			}

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

			if (strVal[strVal.Length - 1] == 'M')
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

		bool ThrowError(string message, Token token = null)
		{
			if (token == null)
				token = tokens[iT];
			throw new CompilerException(message, token, filePath);
		}

		byte RegisterNumber(Registers register)
		{
			switch (register)
			{
				case Registers.Al:
				case Registers.Ax:
				case Registers.Eax:
					return 0;
				case Registers.Cl:
				case Registers.Cx:
				case Registers.Ecx:
					return 1;
				case Registers.Dl:
				case Registers.Dx:
				case Registers.Edx:
					return 2;
				case Registers.Bl:
				case Registers.Bx:
				case Registers.Ebx:
					return 3;
				case Registers.Sp:
				case Registers.Esp:
				case Registers.Ah:
					return 4;
				case Registers.Bp:
				case Registers.Ebp:
				case Registers.Ch:
					return 5;
				case Registers.Si:
				case Registers.Esi:
				case Registers.Dh:
					return 6;
				case Registers.Di:
				case Registers.Edi:
				case Registers.Bh:
					return 7;
			}

			ThrowError("Compiler error: cannot translate register: " + register.ToString());
			return 0xFF;
		}

		byte MemoryRegisterNumber(Registers register)
		{
			if (bits == Bits.Bits32)
			{
				switch (register)
				{
					case Registers.Al:
					case Registers.Ax:
					case Registers.Eax:
						return 0;
					case Registers.Cl:
					case Registers.Cx:
					case Registers.Ecx:
						return 1;
					case Registers.Dl:
					case Registers.Dx:
					case Registers.Edx:
						return 2;
					case Registers.Bl:
					case Registers.Bx:
					case Registers.Ebx:
						return 3;
					case Registers.Sp:
					case Registers.Esp:
					case Registers.Ah:
						return 4;
					case Registers.Bp:
					case Registers.Ebp:
					case Registers.Ch:
						return 5;
					case Registers.Si:
					case Registers.Esi:
					case Registers.Dh:
						return 6;
					case Registers.Di:
					case Registers.Edi:
					case Registers.Bh:
						return 7;
				}
			}
			else if (bits == Bits.Bits16)
			{
				switch (register)
				{
					case Registers.Si:
						return 4;
					case Registers.Di:
						return 5;
					case Registers.Bx:
						return 7;
				}
			}

			ThrowError("Compiler error: cannot translate register: " + register.ToString());
			return 0xFF;
		}


		override internal bool Compile(string filePath)
		{
			this.filePath = filePath;
			tokens = new List<Token>();
			labels = new List<Label>();
			List<FeedMe> feedMes = new List<FeedMe>();
			List<StringData> stringData = new List<StringData>();
			Format format = Format.Flat;
			bool isDataWritten = false;

			LexerParser.LexerParse(filePath, tokens);

			outputFilePath = Path.ChangeExtension(filePath, ".bin");



			bool AddError(string message, Token token)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Error,
					message = message,
					token = token,
					filename = filePath,
				});
				return false;
			}

			Token GetNextToken()
			{
				iT++;
				Token tok = tokens[iT];
				return tok;
			}

			void RequireToken(string token)
			{
				Token tok = GetNextToken();
				if (tok.token != token)
					ThrowError("Expected “" + token + "”, got: “" + tok.token + "”");
			}

			void RequireEitherToken(params string[] accepts)
			{
				Token tok = GetNextToken();
				if (Array.IndexOf(accepts, tok.token) == -1)
					ThrowError("Expected “" + accepts[0] + "”, got: “" + tok.token + "”");
			}

			Side RequireSide(byte width)
			{
				Side side = new Side();

				if (PeekNext() == "[")
				{
					GetNextToken();
					side.isMemoryAccess = true;
				}

				Token next = GetNextToken();

				if (TryAddLabel(next, out side.offset, width, out side.feedMe))
				{

				}
				else if (TryAddStringLit(next, out side.offset, width, out side.feedMe))
				{

				}
				else if (TryIntLiteralParse(next.token, out side.offset))
				{

				}
				else if (TryRegister(next, out side.reg, out side.width))
				{

				}
				else
				{
					side.offset = (uint)ConstMathParse(next);
					// ThrowError("Expected register, label or literal, got: “" + next.token + "”");
				}

				if (side.isMemoryAccess)
				{
					if (PeekNext() == "+")
					{
						GetNextToken();
						next = GetNextToken();
						if (TryIntLiteralParse(next.token, out side.offset) == false)
							ThrowError("Expected offset (integer literal), got: “" + next.token + "”");
					}

					RequireToken("]");
				}

				return side;
			}

			bool ThrowError(string message, Token token = null)
			{
				if (token == null && iT < tokens.Count)
					token = tokens[iT];
				throw new CompilerException(message, token, filePath);
			}

			UInt32 RequireLiteral(byte byteLen)
			{
				Token tok = GetNextToken();
				FeedMe feedMe;
				UInt32 ret;
				if (TryAddLabel(tok, out ret, byteLen, out feedMe))
				{
					return ret;
				}
				else if (TryAddStringLit(tok, out ret, byteLen, out feedMe))
				{
					return ret;
				}
				else
				{
					UInt32 res;
					if (TryIntLiteralParse(tok.token, out res) == false)
					{
						ThrowError("Expected numerical or string literal, got: “" + tok.token + "”");
					}

					// @TODO check size (byteLen)

					return res;
				}
			}

			string RequireStringLiteral()
			{
				Token tok = GetNextToken();
				if (tok.token.Length > 1 &&
						((tok.token[0] == '"'
						&& tok.token[tok.token.Length - 1] == '"')
						|| (tok.token[0] == '\''
						&& tok.token[tok.token.Length - 1] == '\'')))
				{
					string str = tok.token.Substring(1, tok.token.Length - 2);
					return str;
				}

				ThrowError("Expected string literal, got: " + tok.token);
				return null;
			}

			UInt32 RequireIntegerLiteral()
			{
				Token tok = GetNextToken();
				UInt32 res;
				if (TryIntLiteralParse(tok.token, out res) == false)
				{
					ThrowError("Expected integer literal, got: “" + tok.token + "”");
				}
				return res;
			}

			UInt32 RequireLabel(int byteLen, bool absolute = true)
			{
				Token tok = GetNextToken();
				if (TryLabel(tok) == false)
					ThrowError($"Expected label, got “{tok.token}”");

				AddLabelRef(tok, byteLen, absolute, out FeedMe feedMe);
				return (UInt32)Placeholders.Label;
			}

			Registers RequireRegister(out byte width)
			{
				Registers reg;
				Token next = GetNextToken();

				if (TryRegister(next, out reg, out width) == false)
					ThrowError("Expected register, got: " + next.token);

				return reg;
			}

			bool TryRegister(Token next, out Registers reg, out byte width)
			{
				string str = next.token;

				if (str[0] == '%')
					str = str.Substring(1);

				width = 0;

				if (Enum.TryParse(str, true, out reg) == false || Enum.IsDefined(typeof(Registers), reg) == false)
					return false;

				switch (reg)
				{
					case Registers.Al:
					case Registers.Cl:
					case Registers.Bl:
					case Registers.Dl:
					case Registers.Ah:
					case Registers.Ch:
					case Registers.Dh:
					case Registers.Bh:
						width = 1;
						break;
					case Registers.Ax:
					case Registers.Cx:
					case Registers.Dx:
					case Registers.Bx:
					case Registers.Sp:
					case Registers.Bp:
					case Registers.Si:
					case Registers.Di:
						width = 2;
						break;
					case Registers.Eax:
					case Registers.Ecx:
					case Registers.Edx:
					case Registers.Ebx:
					case Registers.Esp:
					case Registers.Ebp:
					case Registers.Esi:
					case Registers.Edi:
						width = 4;
						break;
				}

				return true;
			}

			string GetNext()
			{
				iT++;
				Token tok = tokens[iT];
				return tok.token;
			}

			string PeekNext()
			{
				if (iT + 1 >= tokens.Count)
					return null;

				Token tok = tokens[iT + 1];
				return tok.token;
			}

			bool TryStringLit(Token tok, out string str)
			{
				if (tok.token.Length > 1 &&
						((tok.token[0] == '"'
						&& tok.token[tok.token.Length - 1] == '"')
						|| (tok.token[0] == '\''
						&& tok.token[tok.token.Length - 1] == '\'')))
				{
					str = tok.token.Substring(1, tok.token.Length - 2);
					return true;
				}

				str = null;
				return false;
			}

			bool TryAddStringLit(Token tok, out UInt32 val, byte byteLen, out FeedMe feedMe)
			{
				if (TryStringLit(tok, out string str))
				{
					StringData strData = new StringData
					{
						str = str,
					};
					stringData.Add(strData);
					feedMe = new FeedMe
					{
						/*symbol = str,*/
						bytePos = writer.BaseStream.Position,
						refType = RefType.String,
						refObj = strData,
						width = byteLen,
						offset = address,
					};
					feedMes.Add(feedMe);
					val = (UInt32)Placeholders.String;
					return true;
				}

				feedMe = null;
				val = 0;
				return false;
			}

			bool TryLabel(Token tok)
			{
				if (tok.token[0] == '.')
				{
					return true;
				}
				return false;
			}

			bool TryAddLabel(Token tok, out UInt32 val, byte byteLen, out FeedMe feedMe)
			{
				if (TryLabel(tok))
				{
					val = AddLabelRef(tok, byteLen, true, out feedMe);
					return true;
				}
				feedMe = null;
				val = 0;
				return false;
			}

			void Write(UInt32 ii, int writeWidth)
			{
				switch (writeWidth)
				{
					case 4:
						writer.Write((UInt32)ii);
						break;
					case 2:
						writer.Write((UInt16)ii);
						break;
					case 1:
						writer.Write((byte)ii);
						break;
					default:
						ThrowError("Compiler error: Invalid Write width.");
						break;
				}
			}

			UInt32 AddLabelRef(Token tok, int writeWidth, bool absolute, out FeedMe feedMe)
			{
				feedMe = new FeedMe
				{
					token = tok,
					bytePos = writer.BaseStream.Position,
					refType = RefType.Label,
					subType = (absolute ? RefSubType.Absolute : RefSubType.Relative),
					width = (byte)writeWidth,
					offset = address,
				};
				feedMes.Add(feedMe);

				return (UInt32)Placeholders.Label;
			}

			void AddVarRef(Token tok, int writeWidth)
			{
				feedMes.Add(new FeedMe
				{
					token = tok,
					bytePos = writer.BaseStream.Position,
					refType = RefType.Const,
					width = (byte)writeWidth,
				});

				UInt32 ii = (UInt32)Placeholders.Variable;
				Write(ii, writeWidth);
			}

			void ConstCreate(Token tok, int val)
			{
				consts.Add(new Const
				{
					token = tok,
					val = val,
				});
			}

			void WriteData()
			{
				foreach (var strData in stringData)
				{
					strData.bytePos = writer.BaseStream.Position + 1; // @TODO C# length-prefixed strings?
					writer.Write(strData.str);
					writer.Write((byte)0);
				}
				isDataWritten = true;
			}




			try
			{
				

				for (; iT < tokens.Count; iT++)
				{
					string token = tokens[iT].token;
					Token tok = tokens[iT];
					Instructions instruction;

					if (token[0] == '.')
					{
						RequireToken(":");
						if (writer == null)
							writer = new BinaryWriter(File.Open(outputFilePath, FileMode.Create), Encoding.Default);
						labels.Add(new Label { bytePos = writer.BaseStream.Position, symbol = token });
						continue;
					}
					else if (token[0] == '#')
					{
						int put;

						switch (token)
						{
							case "#bits":
								UInt32 bitNum = RequireIntegerLiteral();
								switch (bitNum)
								{
									case 16:
										bits = Bits.Bits16;
										break;
									case 32:
										bits = Bits.Bits32;
										break;
									default:
										ThrowError("Invalid #bits value.");
										break;
								}
								continue;
							case "#extension":
								string ext = RequireStringLiteral();
								if (writer == null)
									outputFilePath = Path.ChangeExtension(filePath, ext);
								else
									ThrowError("Can't change the extension after file writing has started.");
								continue;
							case "#format":
								{
									string next = GetNext();
									if (Enum.TryParse(next, out format) == false)
										ThrowError("Expected format, got " + next);
									continue;
								}
							case "#address":
								{
									address = RequireIntegerLiteral();
									continue;
								}
							case "#Align":
								{
									uint alignBy = RequireIntegerLiteral();
									long left = (writer.BaseStream.Position) % alignBy;
									if (left != 0)
									{
										left = alignBy - left;
										for (int j = 0; j < left; j++)
											writer.Write((byte)0x90);
									}
									continue;
								}
							case "#Put1":
								put = 1;
								goto PutSub;
							case "#Put2":
								put = 2;
								goto PutSub;
							case "#Put4":
								put = 4;
								goto PutSub;
							case "#Put":
								put = ConstMathParse();
								RequireToken(":");
							PutSub:
								{
									int wasPut = put;

									if (writer == null)
										writer = new BinaryWriter(File.Open(outputFilePath, FileMode.Create), Encoding.Default);

									Token next = GetNextToken();
									if (TryStringLit(next, out string str)) // string lit
									{
										/*if (str.Length > put)
											ThrowError("“" + str + "” is longer than " + put);*/

										for (int j = 0; j < str.Length; j++)
										{
											writer.Write((byte)str[j]);
											put--;
										}

										for (int j = 0; j < put; j++)
										{
											writer.Write((byte)0);
										}
									}
									else if (TryLabel(next)) // label
									{
										Write(AddLabelRef(next, put, true, out FeedMe feedMe), put);
									}
									else if (TryIntLiteralParse(next.token, out UInt32 ii)) // int lit
									{
										if (put > 4)
										{
											for (int j = 0; j < put; j++)
											{
												writer.Write((byte)ii);
											}
										}
										else
										{
											Write(ii, put);
										}
									}
									else // var
									{
										AddVarRef(next, put);
									}

									if (PeekNext() == ",")
									{
										GetNext();
										put = wasPut;
										goto PutSub;
									}
									continue;
								}
							case "#PutData":
							case "#WriteData":
								WriteData();
								continue;
							default:
								ThrowError("Invalid compiler directive.");
								break;
						}
					}

					if (PeekNext() == "=")
					{
						Token varTok = tok;
						RequireToken("=");
						int val = ConstMathParse();
						ConstCreate(tok, val);
						continue;
					}

					if (Enum.TryParse(token, true, out instruction) == false || Enum.IsDefined(typeof(Instructions), instruction) == false) // @TODO integers get parsed as enum
					{
						return ThrowError("Invalid instruction.");
					}

					if (writer == null)
						writer = new BinaryWriter(File.Open(outputFilePath, FileMode.Create), Encoding.Default);

					byte forceWidth = 0;

					switch (instruction)
					{
						case Instructions.Ret:
							if (TryIntLiteralParse(PeekNext(), out UInt32 ii))
							{
								GetNextToken();
								writer.Write((byte)0xC2);
								Write(ii, 2);
							}
							else
							{
								writer.Write((byte)0xc3);
							}
							break;
						case Instructions.Cli:
							writer.Write((byte)0xfa);
							break;
						case Instructions.Hlt:
							writer.Write((byte)0xf4);
							break;
						case Instructions.LodsB:
							writer.Write((byte)0xAC);
							break;
						case Instructions.Inc:
							writer.Write((byte)(0x40 + RegisterNumber(RequireRegister(out forceWidth))));
							break;
						case Instructions.Pop:
							writer.Write((byte)(0x58 + RegisterNumber(RequireRegister(out forceWidth))));
							break;
						case Instructions.Int:
							writer.Write((byte)0xcd);
							Write(RequireIntegerLiteral(), 1);
							break;
						case Instructions.MovB:
							forceWidth = 1; // @TODO enforce forceWidth
							goto MovCommon;
						case Instructions.Mov:
						MovCommon:
							{
								Side dest = RequireSide(0);

								RequireEitherToken("=", ",");

								Side src = RequireSide(dest.width);

								byte opcode;

								if (dest.reg != Registers.None && src.reg == Registers.None && src.isMemoryAccess == false && dest.isMemoryAccess == false)
								{
									switch (dest.width)
									{
										case 1:
											opcode = 0xb0; // MovRImmB
											break;
										case 2:
											if (bits == Bits.Bits32)
												ThrowError("Compiler error: 16-bit Mov in 32-bit code not implemented. Sorry!"); // @TODO 16bit vs 32bit

											opcode = 0xb8; // MovRImmW
											break;
										case 4:
											opcode = 0xb8; // MovRImmL
											break;
										default:
											ThrowError($"Compiler error: Invalid register width: “{dest}” of width {dest.width}.");
											break;
									}

									writer.Write((byte)(opcode + RegisterNumber(dest.reg)));
									if (src.feedMe != null)
										src.feedMe.bytePos++;
									Write(src.offset, dest.width);
								}
								else if (dest.isMemoryAccess && src.isMemoryAccess)
								{
									ThrowError("Invalid instruction – cannot access memory on both sides.");
								}
								else if (src.isMemoryAccess)
								{
									// @TODO @cleanup dupl
									byte modRegRM;
									byte? write1 = null;
									UInt32? write4 = null;

									if (src.reg == Registers.None)
									{
										modRegRM = (byte)ModRegRM.RMemImm;
										write4 = src.offset;
									}
									else if (src.offset != 0 || src.reg == Registers.Ebp)
									{
										if (src.offset > -127 && src.offset < 128)
										{
											modRegRM = (byte)ModRegRM.ROffset1RMem;
											write1 = (byte)src.offset;
										}
										else
										{
											modRegRM = (byte)ModRegRM.ROffset4RMem;
											write4 = src.offset;
										}
										modRegRM |= (byte)(RegisterNumber(src.reg));
										
									}
									else
									{
										modRegRM = (byte)ModRegRM.RRMem;
										modRegRM |= (byte)(RegisterNumber(src.reg));
									}

									modRegRM |= (byte)(RegisterNumber(dest.reg) << 3);

									switch (dest.width)
									{
										case 1:
											opcode = 0x8A;
											break;
										case 2:
											if (bits == Bits.Bits32)
												ThrowError("Compiler error: 16-bit Mov in 32-bit code not implemented. Sorry!"); // @TODO 16bit vs 32bit

											opcode = 0x8B;
											break;
										case 4:
											opcode = 0x8B;
											break;
										default:
											ThrowError($"Compiler error: Invalid register width: “{dest.reg}” of width {dest.width}.");
											break;
									}

									writer.Write(opcode);
									writer.Write(modRegRM);

									if (src.feedMe != null)
										src.feedMe.bytePos += 2;

									if (write1 != null)
										writer.Write((byte)write1);
									if (write4 != null)
										writer.Write((UInt32)write4);
								}
								else if (dest.isMemoryAccess && src.reg == Registers.None)
								{
									byte modRegRM = (byte)((byte)ModRegRM.RRMem | RegisterNumber(dest.reg));
									if (src.offset < 256)
									{
										writer.Write((byte)0xC6);
										writer.Write(modRegRM);
										writer.Write((byte)src.offset);
									}
									else
									{
										writer.Write((byte)0xC7);
										writer.Write(modRegRM);
										writer.Write((UInt32)src.offset);
									}
								}
								else if (dest.isMemoryAccess && src.reg != Registers.None)
								{
									// @TODO @cleanup dupl
									byte modRegRM;
									byte? write1 = null;
									UInt32? write4 = null;

									if (dest.reg == Registers.None)
									{
										modRegRM = (byte)ModRegRM.RMemImm;
										write4 = dest.offset;
									}
									else if (dest.offset != 0 || dest.reg == Registers.Ebp)
									{
										if (dest.offset > -127 && dest.offset < 128)
										{
											modRegRM = (byte)ModRegRM.ROffset1RMem;
											write1 = (byte)dest.offset;
										}
										else
										{
											modRegRM = (byte)ModRegRM.ROffset4RMem;
											write4 = dest.offset;
										}
										modRegRM |= (byte)(RegisterNumber(dest.reg));

									}
									else
									{
										modRegRM = (byte)ModRegRM.RRMem;
										modRegRM |= (byte)(RegisterNumber(dest.reg));
									}

									modRegRM |= (byte)(RegisterNumber(src.reg) << 3);

									switch (src.width)
									{
										case 1:
											opcode = 0x88;
											break;
										case 2:
											if (bits == Bits.Bits32)
												ThrowError("Compiler error: 16-bit Mov in 32-bit code not implemented. Sorry!"); // @TODO 16bit vs 32bit

											opcode = 0x89;
											break;
										case 4:
											opcode = 0x89;
											break;
										default:
											ThrowError($"Compiler error: Invalid register width: “{src.reg}” of width {src.width}.");
											break;
									}

									writer.Write(opcode);
									writer.Write(modRegRM);

									if (dest.feedMe != null)
										dest.feedMe.bytePos += 2;

									if (write1 != null)
										writer.Write((byte)write1);
									if (write4 != null)
										writer.Write((UInt32)write4);
								}
								else if (dest.reg != Registers.None && src.reg != Registers.None)
								{
									byte modRegRM = (byte)((byte)ModRegRM.RToR | RegisterNumber(dest.reg) | (RegisterNumber(src.reg) << 3));

									switch (dest.width)
									{
										case 1:
											opcode = 0x88;
											break;
										case 2:
											if (bits == Bits.Bits32)
												ThrowError("Compiler error: 16-bit Mov in 32-bit code not implemented. Sorry!"); // @TODO 16bit vs 32bit

											opcode = 0x89;
											break;
										case 4:
											opcode = 0x89;
											break;
										default:
											ThrowError($"Compiler error: Invalid register width: “{dest}” of width {dest.width}.");
											break;
									}

									writer.Write(opcode);
									writer.Write(modRegRM);
								}
								else
								{
									ThrowError("Compiler error: invalid instruction.");
								}

								break;
							}
						case Instructions.PushL:
							forceWidth = 4; // @TODO enforce forceWidth
							goto PushCommon;
						case Instructions.Push:
						PushCommon:
							{
								Side side = RequireSide(0);

								if (side.isMemoryAccess)
									ThrowError("Not implemented. Sorry!");

								if (side.reg != Registers.None)
								{
									writer.Write((byte)(0x50 + RegisterNumber(side.reg)));
								}
								else
								{
									if (forceWidth == 0) {
										if (bits == Bits.Bits16)
											forceWidth = 2;
										else
											forceWidth = 4;
									}

									writer.Write((byte)0x68);
									if (side.feedMe != null)
									{
										side.feedMe.bytePos += 1;
										side.feedMe.width = forceWidth;
									}
									Write(side.offset, forceWidth);
								}


								//Write(RequireLiteral(forceWidth), forceWidth);
								break;
							}
						case Instructions.CallL:
							{
								// FF /2
								writer.Write((byte)0xFF);
								writer.Write((byte)((byte)ModRegRM.RMemImm | 0b00_010_000));
								RequireToken("[");
								Write(RequireLiteral(4), 4);
								RequireToken("]");
								break;
							}
						case Instructions.CmpB:
							{
								byte modRegRM = (byte)ModRegRM.RToR | (7 << 3);
								Registers dest = RequireRegister(out byte width);
								modRegRM |= (byte)RegisterNumber(dest);
								if (width != 1)
									ThrowError("Non-matching register width.");

								RequireToken(",");
								writer.Write((byte)0x80);
								writer.Write((byte)modRegRM);
								Write(RequireLiteral(width), width);

								break;
							}
						case Instructions.Je:
							writer.Write((byte)0x74);
							goto JmpCommon;
						case Instructions.Jne:
							writer.Write((byte)0x75);
							goto JmpCommon;
						case Instructions.Jmp:
							writer.Write((byte)0xEB);

							JmpCommon:
							{
								Write(RequireLabel(1, false), 1);
							}
							break;
						case Instructions.Call:
							{
								byte width;
								if (bits == Bits.Bits16)
									width = 2;
								else
									width = 4;
								writer.Write((byte)0xE8);

								Write(RequireLabel(width, false), width);
							}
							break;
						default:
							return ThrowError("Compiler error: Instruction not implemented. Sorry!");
					}
				}

				if (isDataWritten == false)
				{
					WriteData();
				}

				foreach (var feedMe in feedMes)
				{
					switch (feedMe.refType)
					{
						case RefType.Label:
							{
								Label found = labels.Find(x => x.symbol == feedMe.token.token);
								if (found == null)
									ThrowError("Undeclared label: “" + feedMe.token.token + "”", feedMe.token);

								writer.Seek((int)feedMe.bytePos, SeekOrigin.Begin); // @WTF Seek wants int but Position is long??
								if (feedMe.subType == RefSubType.Absolute)
									Write((UInt32)found.bytePos + feedMe.offset, feedMe.width);
								else
									Write((uint)(found.bytePos - (feedMe.bytePos + feedMe.width)), feedMe.width);

								break;
							}
						case RefType.String:
							writer.Seek((int)feedMe.bytePos, SeekOrigin.Begin);
							Write((UInt32)(feedMe.refObj.bytePos + feedMe.offset), feedMe.width);
							break;
						case RefType.Const:
							{
								Const found = consts.Find(x => x.token.token == feedMe.token.token);
								if (found == null)
									ThrowError("Undeclared constant: “" + feedMe.token.token + "”", feedMe.token);

								writer.Seek((int)feedMe.bytePos, SeekOrigin.Begin);
								Write((UInt32)(found.val), feedMe.width);
								break;
							}
						default:
							ThrowError("Compiler error: unknown feedMe.refType.");
							break;
					}
				}
			}
			catch (CompilerException ex)
			{
				return AddError(ex.Message, ex.token);
			}
			finally
			{
				if (writer != null)
					writer.Dispose();
			}

			return true;
		}
	}
}
