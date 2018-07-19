using Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZM01
{
	class ZMAsm
	{
		internal List<Token> tokens;
		public List<OutputMessage> outputMessages = new List<OutputMessage>();

		internal bool Compile(string filePath)
		{
			tokens = new List<Compiler.Token>();

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

			void RequireRg()
			{

			}

			Bitcode RequireReg()
			{
				Token tok = GetToken();
				Bitcode bitcode;
				if (Enum.TryParse(tok.token, out bitcode) == false)
				{
					ThrowError("Expected register, got: “" + tok.token + "”");
				}
				if (bitcode < Bitcode.rz || bitcode > Bitcode.r7)
				{
					ThrowError("Expected register, got: “" + tok.token + "”");
				}
				return bitcode;
			}

			void RequireR3s()
			{
				
			}

			void ParseLiteral(int byteLen)
			{
				Token tok = GetToken();
				if (tok.token.Length > 1 &&
					tok.token[0] == '"'
					&& tok.token[tok.token.Length - 1] == '"')
				{
					// @TODO add string
				}
				else
				{
					// @TODO try parse int
				}
			}

			bool ThrowError(string message)
			{
				throw new CompilerException(message, tokens[i], filePath);
			}


			using (BinaryWriter writer = new BinaryWriter(File.Open(outputFile, FileMode.Create), Encoding.Default)) {

				try
				{
					for (; i < tokens.Count; i++)
					{
						string token = tokens[i].token;
						Token tok = tokens[i];
						Bitcode bitcode;

						if (Enum.TryParse(token, out bitcode) == false)
						{
							// @TODO parse .Labels:
							return ThrowError("Invalid instruction.");
						}

						byte lsb = BitcodeInfo.lsb[bitcode - Bitcode.nop];

						switch (bitcode)
						{
							case Bitcode.nop: // 00 000 000			nop		
							case Bitcode.hlt: // 00 001 000 			hlt		
							case Bitcode.cli: // 00 010 000 			cli		
							case Bitcode.sti: // 00 011 000 			sti		
								break;
							case Bitcode.jmp: // 00 100 000 			jmp	Imm8/1	
							case Bitcode.jne: // 00 110 000 			jne	Imm8/1	
							case Bitcode.je: // 00 111 000 			je	Imm8/1	
								ParseLiteral(1);
								break;
							case Bitcode.movRsR: // 01 reg 000	00001 reg		movRsR	R3	R3s
							case Bitcode.cmpRs: // 01 reg 000	00010 reg		cmpRs	R3	R3s
							case Bitcode.addRs: // 01 reg 000	00011 reg		addRs	R3	R3s
							case Bitcode.ldrRsB: // 01 reg 000	00100 reg		ldrRsB	R3	R3s
							case Bitcode.strRsB: // 01 reg 000	00101 reg		strRsB	R3	R3s
							case Bitcode.ldrRsL: // 01 reg 000	00110 reg		ldrRsL	R3	R3s
							case Bitcode.strRsL: // 01 reg 000	00111 reg		strRsL	R3	R3s
								RequireR3s();
								RequireToken("<<"); // @TODO
								RequireReg();
								break;
							case Bitcode.not: // 01 reg 000	01000 reg		not	R3	R3
							case Bitcode.xor: // 01 reg 000	01001 reg		xor	R3	R3
							case Bitcode.mul: // 01 reg 000	01010 reg		mul	R3	R3
							case Bitcode.div: // 01 reg 000	01011 reg		div	R3	R3
							case Bitcode.sub: // 01 reg 000	01100 reg		sub	R3	R3
							case Bitcode.or: // 01 reg 000	01101 reg		or	R3	R3
							case Bitcode.and: // 01 reg 000	01110 reg		and	R3	R3
								RequireReg();
								RequireToken(","); // @TODO
								RequireReg();
								break;
							case Bitcode.addImmB: // 10 reg 000			addImmB	R3	Imm8/1
							case Bitcode.movRImmL: // 11 reg 000		3×8x	movRImmL	R3	Imm32/4
								Bitcode target = RequireReg();
								lsb |= (byte)((byte)target << 3);
								RequireToken("<<");
								if (bitcode == Bitcode.addImmB)
									ParseLiteral(1);
								else
									ParseLiteral(4);
								break;
							case Bitcode.movRR: // rg reg 001			movRR	R3	R2
							case Bitcode.cmp: // rg reg 010			cmp	R3	R2
							case Bitcode.add: // rg reg 011			add	R3	R2
							case Bitcode.ldrB: // rg reg 100			ldrB	R3	R2
							case Bitcode.strB: // rg reg 101			strB	R3	R2
							case Bitcode.ldrL: // rg reg 110			ldrL	R3	R2
							case Bitcode.strL: // rg reg 111			strL	R3	R2
								RequireReg();
								RequireToken(">>"); // @TODO
								RequireRg();
								break;
							default:
								ThrowError("Unknown instruction: " + token);
								break;
						}

						writer.Write(lsb);
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
