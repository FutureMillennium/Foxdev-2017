using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZM01
{
	public partial class EmulatorForm : Form
	{
		internal byte[] file;
		UInt32 ip = 0;
		UInt32[] registerFile = new UInt32[8];
		bool zf = false;

		public EmulatorForm()
		{
			InitializeComponent();
			this.Icon = Uide.Properties.Resources.Foxdev;
		}

		void Step()
		{
			byte b = file[ip];
			Instruction ins = (Instruction)(b & 0b00_000_111);

			switch (ins)
			{
				case Instruction.nop:
					if ((b & (0b11 << 6)) == 0)
					{
						ins = (Instruction)b;
						switch (ins)
						{
							case Instruction.nop:
								// @TODO
								break;
							case Instruction.hlt:
								// @TODO
								break;
							case Instruction.cli:
								// @TODO
								break;
							case Instruction.sti: // @TODO
								break;
							case Instruction.jmp: // @TODO
								ip++;
								ip = (UInt32)(ip + (sbyte)(file[ip]));
								break;
							case Instruction.jne: // @TODO
								break;
							case Instruction.je: // @TODO
								if (zf)
								{
									ip++;
									ip = (UInt32)(ip + (sbyte)(file[ip]));
								}
								break;
						}
					}
					else
					{
						ins = (Instruction)(b & 0b11_000_111);
						switch (ins)
						{
							case Instruction.movRsR:
								// @TODO parse second byte (MSB)
								break;
							case Instruction.addImmB:
								{
									int reg = (b & 0b111000) >> 3;
									if (reg != 0) // rz
										registerFile[reg] = (UInt32)(registerFile[reg] + (sbyte)(file[ip + 1]));
									ip++;
								}
								break;
							case Instruction.movRImmL:
								{
									int reg = (b & 0b111000) >> 3;
									if (reg != 0) // rz
										registerFile[reg] = BitConverter.ToUInt32(file, (int)ip + 1);
									ip += 4;
								}
								break;
						}
					}
					break;
				case Instruction.movRR:
				case Instruction.cmp:
					{
						int source = (b & 0b111_000) >> 3;
						int target = ((b & 0b11_000_000) >> 6) + 1;
						if (registerFile[source] - registerFile[target] == 0)
							zf = true;
						else
							zf = false;
						break;
					}
				case Instruction.add:
				case Instruction.ldrB:
					{
						int source = (b & 0b111_000) >> 3;
						int target = ((b & 0b11_000_000) >> 6) + 1;
						registerFile[target] = file[registerFile[source]];
						break;
					}
				case Instruction.strB:
					{
						int source = (b & 0b111_000) >> 3;
						int target = ((b & 0b11_000_000) >> 6) + 1;
						if (registerFile[target] >= 0xB8000)
						{
							teletypeTextBox.Text += (char)(registerFile[source]);
						}
						else
						{
							file[registerFile[target]] = (byte)(registerFile[source]);
						}
						break;
					}
				case Instruction.ldrL:
				case Instruction.strL:
					
					break;
			}

			ip++;
		}

		private void stepButton_Click(object sender, EventArgs e)
		{
			Step();
		}
	}
}
