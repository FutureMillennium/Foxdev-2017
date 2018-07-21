using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace ZM01
{
	public partial class EmulatorForm : Form
	{
		const int MEMORY_SIZE = 128 * 1024;

		internal byte[] memoryFile = new byte[MEMORY_SIZE];
		UInt32 ip = 0x100;
		UInt32[] registerFile = new UInt32[10];
		bool zf = false;
		bool isRunning = false;
		bool isStopped = true;
		string teletypeText = "";
		bool iflag = false;
		int tick = 0;
		//bool isHandlingInterrupt = false;
		byte keyboardSettings = 0;
		bool blink = false;

		DispatcherTimer timer = new DispatcherTimer();
		DispatcherTimer uiTimer = new DispatcherTimer();

		public EmulatorForm(byte[] file)
		{
			InitializeComponent();
			this.Icon = Uide.Properties.Resources.Foxdev;
			teletypeBox.KeyPress += TeletypeBox_KeyPress;

			timer.Interval = new TimeSpan(0, 0, 0, 0, 16);
			timer.Tick += Timer_Tick;

			uiTimer.Interval = new TimeSpan(0, 0, 0, 0, 16);
			uiTimer.Tick += UiTimer_Tick;
			uiTimer.IsEnabled = true;

			Array.ConstrainedCopy(file, 0, memoryFile, 0x100, file.Length);

			registerFile[(int)Registers.sp] = MEMORY_SIZE - 1;

			startButton_Click(null, null);
		}

		private void UiTimer_Tick(object sender, EventArgs e)
		{
			teletypeBox.Refresh();

			tick++;
			if (tick == 10)
			{
				blink = !blink;
				tick = 0;
			}
		}

		private void TeletypeBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (isStopped == false && iflag)
			{
				registerFile[(int)Registers.lr] = ip;
				memoryFile[0x80] = (byte)e.KeyChar;
				//Debug.Print(ip.ToString());
				ip = BitConverter.ToUInt32(memoryFile, 0x4);

				if (isRunning == false)
				{
					//isHandlingInterrupt = true;
					isRunning = true;
					Step();
					timer.IsEnabled = true;
				}
			}
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			for (int i = 0; i < 100; i++)
			{
				Step();
				if (isRunning == false)
					break;
				//{
					//teletypeBox.Refresh();
					/*tick = 0;
				}*/
			}

			if (isRunning == false)
			{
				timer.IsEnabled = false;
				if (iflag == false)
				{
					stopButton_Click(null, null);
				}
			}
		}

		void Step()
		{
			byte b = memoryFile[ip];
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
								break;
							case Instruction.hlt:
								isRunning = false;
								break;
							case Instruction.cli:
								iflag = false;
								break;
							case Instruction.sti:
								iflag = true;
								break;
							case Instruction.jmp:
								ip++;
								ip = (UInt32)(ip + (sbyte)(memoryFile[ip]));
								break;
							case Instruction.iret:
								ip = registerFile[(int)Registers.lr];
								//isHandlingInterrupt = false;
								return;
							case Instruction.jne: // @TODO
								break;
							case Instruction.je: // @TODO
								ip++;
								if (zf)
								{
									ip = (UInt32)(ip + (sbyte)(memoryFile[ip]));
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
								throw new Exception("Instruction not implemented."); // @TODO
							case Instruction.addImmB:
								{
									int reg = (b & 0b111000) >> 3;
									if (reg != 0) // rz
										registerFile[reg] = (UInt32)(registerFile[reg] + (sbyte)(memoryFile[ip + 1]));
									ip++;
								}
								break;
							case Instruction.movRImmL:
								{
									int reg = (b & 0b111000) >> 3;
									if (reg != 0) // rz
										registerFile[reg] = BitConverter.ToUInt32(memoryFile, (int)ip + 1);
									ip += 4;
								}
								break;
						}
					}
					break;
				case Instruction.movRR:
					throw new Exception("Instruction not implemented."); // @TODO
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
					throw new Exception("Instruction not implemented."); // @TODO
				case Instruction.ldrB:
					{
						int source = (b & 0b111_000) >> 3;
						int target = ((b & 0b11_000_000) >> 6) + 1;
						registerFile[target] = memoryFile[registerFile[source]];
						break;
					}
				case Instruction.strB:
					{
						int source = (b & 0b111_000) >> 3;
						int target = ((b & 0b11_000_000) >> 6) + 1;
						if (registerFile[target] == 0xC8000)
						{
							keyboardSettings = (byte)(registerFile[source]);
							/*if (keyboardSettings == 1)
								uiTimer.IsEnabled = true;
							else
								uiTimer.IsEnabled = false;*/
						}
						else if (registerFile[target] >= 0xB8000)
						{
							teletypeText += (char)(registerFile[source]);
						}
						else
						{
							memoryFile[registerFile[target]] = (byte)(registerFile[source]);
						}
						break;
					}
				case Instruction.ldrL:
					throw new Exception("Instruction not implemented."); // @TODO
				case Instruction.strL:
					{
						int source = (b & 0b111_000) >> 3;
						int target = ((b & 0b11_000_000) >> 6) + 1;
						Array.ConstrainedCopy(BitConverter.GetBytes(registerFile[source]), 0, memoryFile, (int)registerFile[target], 4);
						break;
					}
			}

			ip++;
		}

		private void stepButton_Click(object sender, EventArgs e)
		{
			Step();
			teletypeBox.Refresh();
		}

		private void startButton_Click(object sender, EventArgs e)
		{
			isRunning = true;
			isStopped = false;
			timer.IsEnabled = true;
			stopButton.Enabled = true;
			startButton.Enabled = false;
			stepButton.Enabled = false;
		}

		private void stopButton_Click(object sender, EventArgs e)
		{
			isRunning = false;
			isStopped = true;
			timer.IsEnabled = false;
			stopButton.Enabled = false;
			startButton.Enabled = true;
			stepButton.Enabled = true;
		}

		private void teletypeBox_Paint(object sender, PaintEventArgs e)
		{
			if (keyboardSettings == 1 && blink == false)
				e.Graphics.DrawString(teletypeText + "_", teletypeBox.Font, Brushes.Black, 0, 0);
			else
				e.Graphics.DrawString(teletypeText, teletypeBox.Font, Brushes.Black, 0, 0);
		}
	}
}
