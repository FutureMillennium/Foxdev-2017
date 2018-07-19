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
		internal List<Compiler.LexerParser.Token> tokens;

		internal bool Compile(string filePath)
		{
			tokens = new List<Compiler.LexerParser.Token>();

			Compiler.LexerParser.LexerParse(filePath, tokens);

			string outputFile = Path.ChangeExtension(filePath, ".zmbin");

			void RequireToken(string token)
			{

			}

			void RequireRg()
			{

			}

			void RequireReg()
			{

			}

			void RequireR3s()
			{

			}

			void ParseLiteral(int byteLen)
			{

			}

			using (BinaryWriter writer = new BinaryWriter(File.Open(outputFile, FileMode.Create), Encoding.Default)) {

				for (int i = 0; i < tokens.Count; i++)
				{
					string token = tokens[i].token;
					Bitcode bitcode;

					if (Enum.TryParse(token, out bitcode) == false)
					{
						return false;
					}

					byte b = (byte)bitcode;

					switch (bitcode)
					{
						case Bitcode.nop:
						case Bitcode.hlt:
						case Bitcode.cli:
						case Bitcode.sti:
							break;
						case Bitcode.movRsR: // 10 001 000	// movRsR	R3	R3s
						case Bitcode.cmpRs: // 10 010 000	// cmpRs	R3	R3s
						case Bitcode.addRs: // 10 011 000	// addRs	R3	R3s
						case Bitcode.subRs: // 10 100 000	// subRs	R3	R3s
						case Bitcode.orRs: // 10 101 000	// orRs	R3	R3s
						case Bitcode.andRs: // 10 110 000	// andRs	R3	R3s
							RequireReg();
							RequireToken("<<"); // @TODO
							RequireR3s();
							break;
						case Bitcode.not: // 00 010 000	// not	R3	R3
						case Bitcode.xor: // 00 011 000	// xor	R3	R3
						case Bitcode.mul: // 00 100 000	// mul	R3	R3
						case Bitcode.div: // 00 101 000	// div	R3	R3
							RequireReg();
							RequireToken("<<"); // @TODO
							RequireReg();
							break;
						case Bitcode.jmpImm: // 00 110 000	jmpImm	Imm8/1
						case Bitcode.jneImm:
						case Bitcode.jeImm:
							ParseLiteral(1);
							break;
						case Bitcode.movRImmB: // 00 111 000 immxxreg	// movRImmB	R3	Imm8/1
						case Bitcode.movRImmL: // 10 111 000	immxxreg	3×8x	// movRImmL	R3	Imm32/4
							RequireReg();
							RequireToken("<<");
							RequireReg();
							break;
						case Bitcode.addImmB: // 011 rg 000	 // addImmB	Imm8/1
						case Bitcode.movRgImmL: // 111 rg 000	// movRgImmL	R2	Imm32/4
							RequireRg();
							RequireToken("<<");
							if (bitcode == Bitcode.addImmB)
								ParseLiteral(1);
							else
								ParseLiteral(4);
							break;
						case Bitcode.movRR: // rg reg 001	// movRR	R3	R2
						case Bitcode.cmp:
						case Bitcode.add:
						case Bitcode.sub:
						case Bitcode.or:
						case Bitcode.and:
							RequireReg();
							RequireToken(">>"); // @TODO
							RequireRg();
							break;
						case Bitcode.ldrB: // 00 reg 111
						case Bitcode.ldrL:
						case Bitcode.strB:
						case Bitcode.strL:
							RequireReg();
							break;
					}

					writer.Write(b);
				}
			}

			return true;
		}
	}
}
