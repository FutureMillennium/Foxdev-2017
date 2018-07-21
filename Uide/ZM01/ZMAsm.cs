using Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZM01
{
	enum RefType { Label, String };
	enum RefSubType { Relative, Absolute };

	class Label
	{
		public long bytePos;
		public string symbol;
	}

	class FeedMe
	{
		public long bytePos;
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

	class ZMAsm
	{
		internal List<Token> tokens;
		public List<OutputMessage> outputMessages = new List<OutputMessage>();
		List<Label> labels;
		UInt32 address = 0x100;

		static internal bool ParseLiteral(string strVal, out UInt32 ii)
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


		internal bool Compile(string filePath)
		{
			tokens = new List<Compiler.Token>();
			labels = new List<Label>();
			List<FeedMe> feedMes = new List<FeedMe>();
			List<StringData> stringData = new List<StringData>(); // @unused

			Compiler.LexerParser.LexerParse(filePath, tokens);

			string outputFile = Path.ChangeExtension(filePath, ".zmbin");

			int i = 0;



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

			Token GetToken()
			{
				i++;
				Token tok = tokens[i];
				return tok;
			}

			void RequireToken(string token)
			{
				Token tok = GetToken();
				if (tok.token != token)
					ThrowError("Expected “" + token + "”, got: “" + tok.token + "”");
			}

			Bitcode RequireRg()
			{
				Bitcode bitcode = RequireReg();
				if (bitcode <= Bitcode.rz || bitcode > Bitcode.r4)
					ThrowError("Register " + bitcode.ToString() + " is not allowed as the target operand.");
				return bitcode;
			}

			Bitcode RequireReg()
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
			}

			void RequireR3s()
			{
				
			}

			bool ThrowError(string message, Token token = null)
			{
				if (token == null)
					token = tokens[i];
				throw new CompilerException(message, token, filePath);
			}


			using (BinaryWriter writer = new BinaryWriter(File.Open(outputFile, FileMode.Create), Encoding.Default)) {


				UInt32 RequireLiteral(int byteLen)
				{
					Token tok = GetToken();
					if (tok.token[0] == '.')
					{
						feedMes.Add(new FeedMe { token = tok, bytePos = writer.BaseStream.Position + 1, refType = RefType.Label, subType = RefSubType.Absolute });
						return 0xFEED14FE; // @TODO placeholder constant?
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
						feedMes.Add(new FeedMe { /*symbol = str,*/ bytePos = writer.BaseStream.Position + 1, refType = RefType.String, refObj = strData });
						return 0xFEED57FE; // @TODO placeholder constant?
					}
					else
					{
						UInt32 res;
						if (ParseLiteral(tok.token, out res) == false)
						{
							ThrowError("Expected numerical or string literal, got: “" + tok.token + "”");
						}

						// @TODO check size (byteLen)

						return res;
					}
				}


				try
				{
					for (; i < tokens.Count; i++)
					{
						string token = tokens[i].token;
						Token tok = tokens[i];
						Bitcode bitcode;
						byte? write1byte = null;
						UInt32? write4bytes = null;

						if (token[0] == '.')
						{
							RequireToken(":");
							labels.Add(new Label { bytePos = writer.BaseStream.Position, symbol = token });
							continue;
						}
						else if (token == "#address")
						{
							RequireToken("=");
							address = RequireLiteral(4);
							continue;
						}

						if (Enum.TryParse(token, out bitcode) == false)
						{
							return ThrowError("Invalid instruction.");
						}

						byte lsb = BitcodeInfo.lsb[bitcode - Bitcode.nop];

						switch (bitcode)
						{
							case Bitcode.nop: // 00 000 000			nop		
							case Bitcode.hlt: // 00 001 000 			hlt		
							case Bitcode.cli: // 00 010 000 			cli		
							case Bitcode.sti: // 00 011 000 			sti		
							case Bitcode.iret:
								break;
							case Bitcode.jmp: // 00 100 000 			jmp	Imm8/1	
							case Bitcode.jne: // 00 110 000 			jne	Imm8/1	
							case Bitcode.je: // 00 111 000 			je	Imm8/1	
								{
									Token label = GetToken();
									if (label.token[0] != '.')
										ThrowError("Expected label, got “" + label.token + "”");

									write1byte = 0xFE;
									feedMes.Add(new FeedMe { bytePos = writer.BaseStream.Position + 1, token = label });
									break;
								}
							case Bitcode.movRsR: // 01 reg 000	00001 reg		movRsR	R3	R3s
							case Bitcode.cmpRs: // 01 reg 000	00010 reg		cmpRs	R3	R3s
							case Bitcode.addRs: // 01 reg 000	00011 reg		addRs	R3	R3s
							case Bitcode.ldrRsB: // 01 reg 000	00100 reg		ldrRsB	R3	R3s
							case Bitcode.strRsB: // 01 reg 000	00101 reg		strRsB	R3	R3s
							case Bitcode.ldrRsL: // 01 reg 000	00110 reg		ldrRsL	R3	R3s
							case Bitcode.strRsL: // 01 reg 000	00111 reg		strRsL	R3	R3s
								{
									ThrowError("Not implemented: “" + token + "”");
									RequireR3s();
									RequireToken("<<"); // @TODO
									RequireReg();
									break;
								}
							case Bitcode.not: // 01 reg 000	01000 reg		not	R3	R3
							case Bitcode.xor: // 01 reg 000	01001 reg		xor	R3	R3
							case Bitcode.mul: // 01 reg 000	01010 reg		mul	R3	R3
							case Bitcode.div: // 01 reg 000	01011 reg		div	R3	R3
							case Bitcode.sub: // 01 reg 000	01100 reg		sub	R3	R3
							case Bitcode.or: // 01 reg 000	01101 reg		or	R3	R3
							case Bitcode.and: // 01 reg 000	01110 reg		and	R3	R3
								{
									ThrowError("Not implemented: “" + token + "”");
									RequireReg();
									RequireToken(","); // @TODO
									RequireReg();
									break;
								}
							case Bitcode.addImmB: // 10 reg 000			addImmB	R3	Imm8/1
							case Bitcode.movRImmL: // 11 reg 000		3×8x	movRImmL	R3	Imm32/4
								{
									Bitcode target = RequireReg();
									lsb |= (byte)((byte)target << 3);
									RequireToken("<<");
									if (bitcode == Bitcode.addImmB)
									{
										RequireToken("+");
										write1byte = (byte)RequireLiteral(1);
									}
									else
										write4bytes = RequireLiteral(4);
									break;
								}
							case Bitcode.movRR: // rg reg 001			movRR	R3	R2
							case Bitcode.cmp: // rg reg 010			cmp	R3	R2
							case Bitcode.add: // rg reg 011			add	R3	R2
								{
									Bitcode target = RequireRg();
									lsb |= (byte)((byte)(target - 1) << 6); // @TODO @cleanup dupl
									Token sep = GetToken();
									switch (sep.token)
									{
										case "<<":
										case "==":
										case ",":
											break;
										default:
											ThrowError("Expected separator, got: “" + sep.token + "”");
											break;
									}
									/*if (bitcode == Bitcode.add)
										RequireToken("+");*/
									Bitcode source = RequireReg();
									lsb |= (byte)((byte)source << 3);
									break;
								}
							case Bitcode.ldrB: // rg reg 100			ldrB	R3	R2
							case Bitcode.ldrL: // rg reg 110			ldrL	R3	R2
								{
									Bitcode target = RequireRg();
									lsb |= (byte)((byte)(target - 1) << 6); // @TODO @cleanup dupl
									Token sep = GetToken();
									switch (sep.token)
									{
										case "<<":
										case ",":
											break;
										default:
											ThrowError("Expected separator, got: “" + sep.token + "”");
											break;
									}
									RequireToken("[");
									Bitcode source = RequireReg();
									lsb |= (byte)((byte)source << 3);
									RequireToken("]");
									break;
								}
							case Bitcode.strB: // rg reg 101			strB	R3	R2
							case Bitcode.strL: // rg reg 111			strL	R3	R2
								{
									RequireToken("[");
									Bitcode target = RequireRg();
									lsb |= (byte)((byte)(target - 1) << 6);
									RequireToken("]");
									Token sep = GetToken();
									switch (sep.token)
									{
										case "<<":
										case ",":
											break;
										default:
											ThrowError("Expected separator, got: “" + sep.token + "”");
											break;
									}
									Bitcode source = RequireReg();
									lsb |= (byte)((byte)source << 3);
									break;
								}
							default:
								ThrowError("Unknown instruction: " + token);
								break;
						}

						writer.Write(lsb);
						if (write1byte != null)
							writer.Write((byte)write1byte);
						if (write4bytes != null)
							writer.Write((UInt32)write4bytes);
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
								Label found = labels.Find(x => x.symbol == feedMe.token.token);
								if (found == null)
									ThrowError("Undeclared label: “" + feedMe.token.token + "”", feedMe.token);

								writer.Seek((int)feedMe.bytePos, SeekOrigin.Begin); // @WTF Seek wants int but Position is long??
								if (feedMe.subType == RefSubType.Absolute)
									writer.Write((UInt32)(found.bytePos + address));
								else
									writer.Write((sbyte)(found.bytePos - (feedMe.bytePos + 1)));

								break;
							case RefType.String:
								writer.Seek((int)feedMe.bytePos, SeekOrigin.Begin);
								writer.Write((UInt32)(feedMe.refObj.bytePos + address));
								break;
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
			}

			return true;
		}
	}
}
