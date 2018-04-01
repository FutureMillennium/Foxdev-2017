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
			long[] sPosList = null;
			List<Tuple<long, int, int>> sRefList = new List<Tuple<long, int, int>>(); // posInFile, iSList, width
			int iLit = 0;
			bool isDataWritten = false;

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

						int width;

						if (curFunction.bits == Bits.Bits32)
							width = 4;
						else if (curFunction.bits == Bits.Bits16)
							width = 2;
						else
						{
							width = 0;
							AddError("???");
						}

						sRefList.Add(new Tuple<long, int, int>(writer.BaseStream.Position, sList.Count - 1, width)); // @TODO length/width
						iLit++;

						iiStringLiteral++;
						if (curFunction.literalReferences.Count > iiStringLiteral)
						{
							iStringLiteral = curFunction.literalReferences[iiStringLiteral].pos;
						}
					}
				}

				void UnresolvedLabelAccept(int bytes)
				{
					if (urNextLabelUnresolved != -1 && i == curFunction.urLabelsUnresolved[urNextLabelUnresolved].pos)
					{
						curFunction.urLabelsUnresolved[urNextLabelUnresolved].bytePos = writer.BaseStream.Position;
						curFunction.urLabelsUnresolved[urNextLabelUnresolved].bytes = bytes;
						if (curFunction.urLabelsUnresolved.Count > urNextLabelUnresolved + 1)
							urNextLabelUnresolved++;
						else
							urNextLabelUnresolved = -1;
					}
				}

				void WriteData()
				{
					int jMax = sList.Count;
					sPosList = new long[jMax];

					for (int j = 0; j < jMax; j++)
					{
						var s = sList[j];

						sPosList[j] = writer.BaseStream.Position + 1; // @TODO non-prefixed strings?
						writer.Write(s);
						writer.Write((byte)0); // @TODO @hack 0/null-terminated string for now
					}

					isDataWritten = true;
				}


				while (i < iMax)
				{
					ByteCode b = curFunction.byteCode[i];

					if (nextLabelI != -1)
						while (i == curFunction.labels[nextLabelI].pos)
						{
							curFunction.labels[nextLabelI].bytePos = writer.BaseStream.Position;
							if (curFunction.labels.Count > nextLabelI + 1)
							{
								nextLabelI++;
							}
							else
							{
								nextLabelI = -1;
								break;
							}
						}

					switch (b)
					{
						case ByteCode.Cli:
							writer.Write((byte)0xfa);
							break;
						case ByteCode.RetNear:
							writer.Write((byte)0xc3);
							break;
						case ByteCode.Hlt:
							writer.Write((byte)0xf4);
							break;
						case ByteCode.LodsB:
							writer.Write((byte)0xAC);
							break;
						case ByteCode.IntImmB:
							writer.Write((byte)0xcd);
							writer.Write((byte)curFunction.byteCode[i + 1]);
							i += 1;
							break;
						case ByteCode.CallRelW:
						case ByteCode.CallRelL:
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

								UnresolvedLabelAccept(bytes);

								if (curFunction.bits == Bits.Bits32)
									writer.Write((uint)curFunction.byteCode[i]);
								else if (curFunction.bits == Bits.Bits16)
									writer.Write((ushort)curFunction.byteCode[i]);
								break;
							}
						case ByteCode.JeRelB:
							writer.Write((byte)0x74);
							goto JmpCommon;
						case ByteCode.JneRelB:
							writer.Write((byte)0x75);
							goto JmpCommon;
						case ByteCode.JmpRelB:
							writer.Write((byte)0xEB);
						JmpCommon:
							i += 1;

							UnresolvedLabelAccept(1);

							writer.Write((byte)curFunction.byteCode[i]);
							break;
						case ByteCode.PushImmW:
							// @TODO Now 16bit only. 32bit
							writer.Write((byte)0x68);
							i += 1;
							sLitAcceptStringLiteral(i);
							writer.Write((ushort)curFunction.byteCode[i]);
							break;
						case ByteCode.PushImmL:
							writer.Write((byte)0x68);
							i += 1;
							sLitAcceptStringLiteral(i);
							writer.Write((uint)curFunction.byteCode[i]);
							break;
						case ByteCode.PopRW: // @TODO 16-bit
						case ByteCode.PopRL:
							writer.Write((byte)(0x58 + RegisterNumber(curFunction.byteCode[i + 1])));
							i += 1;
							break;
						case ByteCode.IncR:
							writer.Write((byte)(0x40 + RegisterNumber(curFunction.byteCode[i + 1])));
							i += 1;
							break;
						case ByteCode.PushRL:
							writer.Write((byte)(0x50 + RegisterNumber(curFunction.byteCode[i + 1])));
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
						case ByteCode.MovRmRB:
							writer.Write((byte)0x88);
							goto MovCommon;
						case ByteCode.MovRmRL:
							writer.Write((byte)0x89);
							goto MovCommon;
						case ByteCode.MovRRmB:
							writer.Write((byte)0x8A);
							goto MovCommon;
						case ByteCode.MovRRmL:
							writer.Write((byte)0x8b);
						MovCommon:

							switch (curFunction.byteCode[i + 1])
							{
								case ByteCode.RRMem:
									{
										byte modRegRm = 0b00_000_000;

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 2]) << 3);

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 3]));

										writer.Write((byte)modRegRm);
										
										i += 3;
										break;
									}
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
								case ByteCode.RMemImm:
									{
										byte modRegRm = 0b00_000_101;

										modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 2]) << 3);

										writer.Write((byte)modRegRm);

										writer.Write((uint)curFunction.byteCode[i + 3]);

										i += 3;

										break;
									}
								// @TODO
								default:
									return AddError("Invalid mod or not implemented.");
							}
							break;
						case ByteCode.MovRmImmB:
							{
								writer.Write((byte)0xc6);

								switch (curFunction.byteCode[i + 1])
								{
									case ByteCode.RRMem:
										{
											byte modRegRm = 0b00_000_000;
											modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 2]));
											writer.Write((byte)modRegRm);

											writer.Write((byte)curFunction.byteCode[i + 3]);

											i += 3;
										}
										break;
									default:
										return AddError("Invalid mod or not implemented. (" + b.ToString() + ")");
								}
							}
							break;
						case ByteCode.CmpRImmB:
							{
								writer.Write((byte)0x80);

								/*switch (curFunction.byteCode[i + 1]) // @TODO
								{
									case ByteCode.RRMem:
										{*/
											byte modRegRm = 0b11_111_000;
											modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 1]));
											writer.Write((byte)modRegRm);

											writer.Write((byte)curFunction.byteCode[i + 2]);

											i += 2;
										/*}
										break;
									default:
										return AddError("Invalid mod or not implemented. (" + b.ToString() + ")");
								}*/
							}
							break;
						case ByteCode.CmpRMemImmB:
							{
								writer.Write((byte)0x80);

								byte modRegRm = 0b00_111_000;
								modRegRm |= (byte)(RegisterNumber(curFunction.byteCode[i + 1]));
								writer.Write((byte)modRegRm);

								writer.Write((byte)curFunction.byteCode[i + 2]);

								i += 2;
							}
							break;
						case ByteCode.Put4BytesHere:
							i++;
							UnresolvedLabelAccept(4);
							writer.Write((uint)curFunction.byteCode[i]);
							break;
						case ByteCode.Align:
							i++;
							uint alignBy = (uint)curFunction.byteCode[i];
							long left = (writer.BaseStream.Position) % alignBy;
							if (left != 0)
							{
								left = alignBy - left;
								for (int j = 0; j < left; j++)
									writer.Write((byte)0x90);
							}
							break;
						case ByteCode.WriteDataHere:
							WriteData();
							break;
						default:
							return AddError(b.ToString() + ": binary compilation not implemented.");
					}

					i++;
				}

				if (isDataWritten == false)
					WriteData();

				iMax = sRefList.Count;

				for (i = 0; i < iMax; i++)
				{
					var r = sRefList[i];

					writer.Seek((int)r.Item1, SeekOrigin.Begin);
					if (r.Item3 == 4)
						writer.Write((uint)(sPosList[r.Item2] + curUnit.relativeAddress));
					else if (r.Item3 == 2)
						writer.Write((ushort)(sPosList[r.Item2] + curUnit.relativeAddress));
					else
						return AddError("Invalid string literal reference size!");
				}

				iMax = curFunction.urLabelsUnresolved.Count;
				for (i = 0; i < iMax; i++)
				{
					var l = curFunction.urLabelsUnresolved[i];

					writer.Seek((int)l.bytePos, SeekOrigin.Begin);
					long val;
					
					if (l.isAbsolute)
						val = l.reference.bytePos + curUnit.relativeAddress;
					else
						val = l.reference.bytePos - (l.bytePos + l.bytes);

					if (l.bytes == 1)
						writer.Write((byte)val);
					else if (l.bytes == 2)
						writer.Write((ushort)val);
					else if (l.bytes == 4)
						writer.Write((uint)val);
					else
						return AddError("Invalid label reference size!");
				}

				writer.Close();
			}

			return true;
		}
	}
}
