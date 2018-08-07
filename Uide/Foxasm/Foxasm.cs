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

	partial class Foxasm : Compiler
	{
		internal List<Token> tokens;
		List<Label> labels;
		List<Const> consts = new List<Const>();
		UInt32 address = 0;
		string filePath;
		int iT = 0;
		BinaryWriter writer = null;

		static internal bool TryIntLiteralParse(string strVal, out UInt32 ii)
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
			return 0xFF;
		}


		override internal bool Compile(string filePath)
		{
			this.filePath = filePath;
			tokens = new List<Token>();
			labels = new List<Label>();
			List<FeedMe> feedMes = new List<FeedMe>();
			List<StringData> stringData = new List<StringData>();
			Bits bits = Bits.Bits32;
			Format format = Format.Flat;

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

			/*Bitcode RequireReg()
			{
				Token tok = GetToken();
				Bitcode bitcode;
				if (Enum.TryParse(tok.token, out bitcode) == false)
				{
					ThrowError("Expected register, got: “" + tok.token + "”");
				}
				if (bitcode < Bitcode.rz || bitcode > Bitcode.sp)
				{
					ThrowError("Expected register, got: “" + tok.token + "”");
				}
				return bitcode;
			}*/

			bool ThrowError(string message, Token token = null)
			{
				if (token == null && iT < tokens.Count)
					token = tokens[iT];
				throw new CompilerException(message, token, filePath);
			}

			UInt32 RequireLiteral(int byteLen)
			{
				Token tok = GetNextToken();
				if (tok.token[0] == '.')
				{
					feedMes.Add(new FeedMe {
						token = tok,
						bytePos = writer.BaseStream.Position + 1,
						refType = RefType.Label,
						subType = RefSubType.Absolute,
						width = (byte)byteLen,
					});
					return (UInt32)Placeholders.Label;
				}
				else if (tok.token.Length > 1 &&
					tok.token[0] == '"'
					&& tok.token[tok.token.Length - 1] == '"')
				{
					string str = tok.token.Substring(1, tok.token.Length - 2);
					StringData strData = new StringData
					{
						str = str,
					};
					stringData.Add(strData);
					feedMes.Add(new FeedMe {
						/*symbol = str,*/
						bytePos = writer.BaseStream.Position + 1,
						refType = RefType.String,
						refObj = strData,
						width = (byte)byteLen,
					});
					return (UInt32)Placeholders.String;
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

			string GetNext()
			{
				iT++;
				Token tok = tokens[iT];
				return tok.token;
			}

			string PeekNext()
			{
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

			bool TryLabel(Token tok)
			{
				if (tok.token[0] == '.')
				{
					return true;
				}
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

			void AddLabelRef(Token tok, int writeWidth)
			{
				feedMes.Add(new FeedMe {
					token = tok,
					bytePos = writer.BaseStream.Position,
					refType = RefType.Label,
					subType = RefSubType.Absolute,
					width = (byte)writeWidth,
				});

				UInt32 ii = (UInt32)Placeholders.Label;
				Write(ii, writeWidth);
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
										AddLabelRef(next, put);
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
							/*case "#DoTimes":
								break;*/
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

					if (Enum.TryParse(token, true, out instruction) == false)
					{
						return ThrowError("Invalid instruction.");
					}

					if (writer == null)
						writer = new BinaryWriter(File.Open(outputFilePath, FileMode.Create), Encoding.Default);

					switch (instruction)
					{
						case Instructions.Ret:
							writer.Write((byte)0xc3);
							break;
						case Instructions.Mov:
							Token next = GetNextToken();
							Registers dest;
							if (Enum.TryParse(next.token, true, out dest) == false)
								ThrowError("Expected register, got: " + next.token);
							RequireToken("=");
							UInt32 val = RequireIntegerLiteral();

							//case ByteCode.MovRImmL:
							writer.Write((byte)(0xb8 + RegisterNumber(dest)));
							writer.Write((uint)val);

							break;
						default:
							return ThrowError("Instruction not implemented. Sorry!");
					}
				}

				foreach (var strData in stringData)
				{
					strData.bytePos = writer.BaseStream.Position + 1; // @TODO
					writer.Write(strData.str);
					writer.Write((byte)0);
				}

				// @TODO:
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
									writer.Write((UInt32)(found.bytePos + address));
								else
									writer.Write((sbyte)(found.bytePos - (feedMe.bytePos + 1)));

								break;
							}
						case RefType.String:
							writer.Seek((int)feedMe.bytePos, SeekOrigin.Begin);
							writer.Write((UInt32)(feedMe.refObj.bytePos + address));
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
