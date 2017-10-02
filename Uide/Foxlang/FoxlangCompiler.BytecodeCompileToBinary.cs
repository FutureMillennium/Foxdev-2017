using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
		bool BytecodeCompileToBinary(string outputFile)
		{
			Function curFunction = entryPoint; // @TODO non-EntryPoint function compile

			List<string> sList = new List<string>();
			long[] sPosList;
			List<Tuple<long, int, int>> sRefList = new List<Tuple<long, int, int>>(); // posInFile, iSList, width
			int iLit = 0;

			bool AddError(string message)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Error,
					message = message,
					filename = outputFile,
				});
				return false;
			}

			int iMax, i;
			int iStringLiteral = -1,
				iiStringLiteral = -1,
				nextLabelI = -1,
				urNextLabelUnresolved = -1;

			iiStringLiteral++;
			if (curFunction.literalReferences.Count > iiStringLiteral)
			{
				iStringLiteral = curFunction.literalReferences[iiStringLiteral].pos;
			}

			if (curFunction.labels.Count > 0)
				nextLabelI = 0;

			if (curFunction.urLabelsUnresolved.Count > 0)
				urNextLabelUnresolved = 0;

			using (BinaryWriter writer = new BinaryWriter(File.Open(outputFile, FileMode.Create), Encoding.Default))
			{
				iMax = curFunction.byteCode.Count;
				i = 0;


				void sLitAcceptStringLiteral(int di)
				{
					if (di == iStringLiteral)
					{
						sList.Add(curFunction.literalReferences[iLit].symbol); // @TODO duplicate literals
						sRefList.Add(new Tuple<long, int, int>(writer.BaseStream.Position, sList.Count - 1, 2)); // @TODO length/width
						iLit++;

						iiStringLiteral++;
						if (curFunction.literalReferences.Count > iiStringLiteral)
						{
							iStringLiteral = curFunction.literalReferences[iiStringLiteral].pos;
						}
					}
				}


				while (i < iMax)
				{
					ByteCode b = curFunction.byteCode[i];

					if (nextLabelI != -1 && i == curFunction.labels[nextLabelI].pos)
					{
						curFunction.labels[nextLabelI].bytePos = writer.BaseStream.Position;
						if (curFunction.labels.Count > nextLabelI + 1)
							nextLabelI++;
						else
							nextLabelI = -1;
					}

					switch (b)
					{
						case ByteCode.Cli:
							writer.Write((byte)0xfa);
							break;
						case ByteCode.Ret:
							writer.Write((byte)0xc3);
							break;
						case ByteCode.Hlt:
							writer.Write((byte)0xf4);
							break;
						case ByteCode.Int:
							writer.Write((byte)0xcd);
							writer.Write((byte)curFunction.byteCode[i + 1]);
							i += 1;
							break;
						case ByteCode.Call:
							{
								writer.Write((byte)0xE8);
								i += 1;

								int bytes;

								if (curFunction.bits == Bits.Bits32)
									bytes = 4;
								else if (curFunction.bits == Bits.Bits16)
									bytes = 2;
								else
									return AddError("???"); // @TODO?

								if (urNextLabelUnresolved != -1 && i == curFunction.urLabelsUnresolved[urNextLabelUnresolved].pos)
								{
									curFunction.urLabelsUnresolved[urNextLabelUnresolved].bytePos = writer.BaseStream.Position;
									curFunction.urLabelsUnresolved[urNextLabelUnresolved].bytes = bytes;
									if (curFunction.urLabelsUnresolved.Count > urNextLabelUnresolved + 1)
										urNextLabelUnresolved++;
									else
										urNextLabelUnresolved = -1;
								}

								if (curFunction.bits == Bits.Bits32)
									writer.Write((uint)curFunction.byteCode[i]);
								else if (curFunction.bits == Bits.Bits16)
									writer.Write((ushort)curFunction.byteCode[i]);
								break;
							}
						case ByteCode.Jmp:
							writer.Write((byte)0xE9);
							i += 1;
							writer.Write((uint)curFunction.byteCode[i]); // @TODO resolve correct address
							break;
						case ByteCode.PushW:
							// @TODO Now 16bit only. 32bit
							writer.Write((byte)0x68);
							i += 1;
							sLitAcceptStringLiteral(i);
							writer.Write((ushort)curFunction.byteCode[i]);
							break;
						case ByteCode.PushL:
							writer.Write((byte)0x68);
							i += 1;
							sLitAcceptStringLiteral(i);
							writer.Write((uint)curFunction.byteCode[i]);
							break;
						case ByteCode.PopRW:
							writer.Write((byte)(0x58 + RegisterNumber(curFunction.byteCode[i + 1])));
							i += 1;
							break;
						case ByteCode.MovRImmB:
							writer.Write((byte)(0xb0 + RegisterNumber(curFunction.byteCode[i + 1])));
							writer.Write((byte)curFunction.byteCode[i + 2]);
							i += 2;
							break;
						case ByteCode.MovRImmW:
							// @TODO 16bit vs 32bit
							writer.Write((byte)(0xb8 + RegisterNumber(curFunction.byteCode[i + 1])));

							sLitAcceptStringLiteral(i + 2);

							writer.Write((ushort)curFunction.byteCode[i + 2]);
							i += 2;
							break;
						case ByteCode.MovRImmL: // @TODO @cleanup
							writer.Write((byte)(0xb8 + RegisterNumber(curFunction.byteCode[i + 1])));

							sLitAcceptStringLiteral(i + 2);

							writer.Write((uint)curFunction.byteCode[i + 2]); //d
							i += 2;
							break;
						case ByteCode.MovRmRL:
							writer.Write((byte)0x89);
							goto MovCommon;
						case ByteCode.MovRRmL:
							writer.Write((byte)0x8b);
						MovCommon:

							switch (curFunction.byteCode[i + 1])
							{
								case ByteCode.RRMemOffset1:
									{
										byte modRegRm = 0b01_000_000;

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 2]) << 3);

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 3]));

										writer.Write((byte)modRegRm);

										writer.Write((byte)curFunction.byteCode[i + 4]);

										i += 4;
										break;
									}
								case ByteCode.RToR:
									{
										byte modRegRm = 0b11_000_000;

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 2]) << 3);

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 3]));

										writer.Write((byte)modRegRm);

										i += 3;

										break;
									}
								// @TODO
								default:
									return AddError("Invalid mod or not implemented.");
							}

							break;
						default:
							return AddError(b.ToString() + ": binary compilation not implemented.");
					}

					i++;
				}

				iMax = sList.Count;
				sPosList = new long[iMax];

				for (i = 0; i < iMax; i++)
				{
					var s = sList[i];

					sPosList[i] = writer.BaseStream.Position + 1; // @TODO non-prefixed strings?
					writer.Write(s);
				}

				iMax = sRefList.Count;

				for (i = 0; i < iMax; i++)
				{
					var r = sRefList[i];

					writer.Seek((int)r.Item1, SeekOrigin.Begin);
					if (r.Item3 == 2)
						writer.Write((ushort)(sPosList[r.Item2] + relativeAddress));
					// else // @TODO
				}

				iMax = curFunction.urLabelsUnresolved.Count;
				for (i = 0; i < iMax; i++)
				{
					var l = curFunction.urLabelsUnresolved[i];

					writer.Seek((int)l.bytePos, SeekOrigin.Begin);
					long val = l.reference.bytePos - (l.bytePos + l.bytes);
					if (l.bytes == 2)
						writer.Write((ushort)val);
					else
						return AddError("Not implemented!");
				}

				writer.Close();
			}

			return true;
		}
	}
}
