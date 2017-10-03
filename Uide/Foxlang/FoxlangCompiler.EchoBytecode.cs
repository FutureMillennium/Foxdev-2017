using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
		enum BytecodeType { Bytecode, Literal }

		public string EchoBytecode()
		{
			Stack<BytecodeType> stack = new Stack<BytecodeType>();

			Function f = entryPoint; // @TODO for more than one function?

			if (f == null)
				return "";

			StringBuilder sb = new StringBuilder();

			int iMax = f.byteCode.Count;
			int i = 0;
			int iLit = 0,
				iURLabels = 0,
				iURVars = 0;
			int untilLine = 0;

			while (i < iMax)
			{
				ByteCode b = f.byteCode[i];

				if (stack.Count > 0 && stack.Pop() == BytecodeType.Literal)
				{
					switch (b)
					{
						case ByteCode.LabelFeedMe: // @TODO Unreliable
												   //sb.AppendLine(b.ToString("x"));
							sb.Append(f.urLabelsUnresolved[iURLabels].symbol);
							iURLabels++;
							break;
						case ByteCode.StringLiteralFeedMe: // @TODO Unreliable
							sb.Append("\"" + f.literalReferences[iLit].symbol + "\"");
							iLit++;
							break;
						case ByteCode.VarFeedMe: // @TODO Unreliable
							sb.Append(f.urVarsUnresolved[iURVars].symbol);
							iURVars++;
							//sb.AppendLine(b.ToString("x"));
							break;
						default:
							sb.Append("0x" + b.ToString("x"));
							break;
					}
				}
				else
				{

					switch (b)
					{


						case ByteCode.RRMemOffset1:
						case ByteCode.RRMemOffset4:
							stack.Push(BytecodeType.Literal);
							stack.Push(BytecodeType.Bytecode);
							stack.Push(BytecodeType.Bytecode);
							untilLine += 1;
							goto default;
						case ByteCode.PopRW:
						case ByteCode.PopRL:
						case ByteCode.IncR:
						case ByteCode.PushRL:
							untilLine = 1;
							goto default;

						case ByteCode.PushImmB:
						case ByteCode.PushImmW:
						case ByteCode.PushImmL:
						case ByteCode.CallRelW:
						case ByteCode.CallRelL:
						case ByteCode.JmpRelB:
						case ByteCode.JeRelB:
						case ByteCode.JneRelB:
						case ByteCode.IntImmB:
						case ByteCode.Put4BytesHere:
						case ByteCode.Align:
							stack.Push(BytecodeType.Literal);
							untilLine = 1;
							goto default;
						case ByteCode.MovRImmL:
						case ByteCode.MovRImmW:
						case ByteCode.MovRImmB:
						case ByteCode.CmpRImmB:
						case ByteCode.CmpRMemImmB:
							stack.Push(BytecodeType.Literal);
							stack.Push(BytecodeType.Bytecode);
							untilLine = 2;
							goto default;
						case ByteCode.MovRmRB:
						case ByteCode.MovRmRW:
						case ByteCode.MovRmRL:
						case ByteCode.MovRRmB:
						case ByteCode.MovRRmW:
						case ByteCode.MovRRmL:
							untilLine = 3;
							goto default;
						case ByteCode.MovRmImmB:
						case ByteCode.MovRmImmL:
							stack.Push(BytecodeType.Literal);
							stack.Push(BytecodeType.Bytecode);
							stack.Push(BytecodeType.Bytecode);
							untilLine = 3;
							goto default;
						default:
							sb.Append(b.ToString());
							break;
					}
				}

				if (untilLine == 0)
					sb.AppendLine();
				else
				{
					//sb.Append('(' + b.ToString("x") + ")");
					sb.Append(' ');
					untilLine--;
				}

				i++;
			}

			return sb.ToString();
		}
	}
}
