using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZM01
{
	class ZMDisassembler
	{
		internal string Disassemble(byte[] file)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < file.Length; i++)
			{
				byte b = file[i];
				int found;
				Bitcode bc;
				string after = null;

				sb.Append(i.ToString("x8") + " ");

				if ((b & 0b0000_0111) == 0)
				{
					if ((b & (0b11 << 6)) == 0)
					{
						found = Array.IndexOf(BitcodeInfo.lsb, b);
						bc = (Bitcode)(found + (int)Bitcode.nop);

						switch (bc)
						{
							case Bitcode.jmp:
							case Bitcode.je:
							case Bitcode.jne:
								i++;
								after = " 0x" + file[i].ToString("x2");
								break;
						}

					}
					else
					{
						// @TODO MSB
						found = Array.IndexOf(BitcodeInfo.lsb, (byte)(b & 0b1100_0111));
					}

					switch ((b & (0b11 << 6)) >> 6)
					{
						case 0b10:
							{
								i++;
								Bitcode source = (Bitcode)((b & (0b111 << 3)) >> 3); // @TODO @cleanup dupl
								after = " " + source + " <<+ 0x" + file[i].ToString("x2");
								break;
							}
						case 0b11:
							{
								Bitcode source = (Bitcode)((b & (0b111 << 3)) >> 3); // @TODO @cleanup dupl
								after = " " + source + " << 0x" + BitConverter.ToUInt32(file, i + 1).ToString("x8");
								i += 4;
								break;
							}
					}
				}
				else
				{
					found = Array.IndexOf(BitcodeInfo.lsb, (byte)(b & 0b0000_0111));
					Bitcode target = (Bitcode)(((b & (0b11 << 6)) >> 6) + 1);
					Bitcode source = (Bitcode)((b & (0b111 << 3)) >> 3);
					after = " " + target + " << " + source;
				}

				if (found > -1)
				{
					bc = (Bitcode)(found + (int)Bitcode.nop);

					sb.Append(bc.ToString());
					if (after != null)
						sb.Append(after);
					sb.Append(Environment.NewLine);
				} else
				{
					sb.Append(b.ToString("x2"));
				}
			}

			return sb.ToString();
		}
	}
}
