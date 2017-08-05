using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Uide
{
	public partial class MainForm : Form
	{
		const string PRODUCT_NAME = "Uide";

		bool isFileLoaded = false, isELFfile;
		string filePath, fileName;
		byte[] file;
		ELFheader32 elfHeader;
		int maxLines = 0, fileLines = 0;
		bool isMultiboot = false;

		Font font = new Font(FontFamily.GenericMonospace, 13);




		public MainForm(string[] args)
		{
			InitializeComponent();

			MainForm_Resize(null, null);

			if (args.Length > 0)
				OpenFile(args[0]);
		}

		private void MainForm_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Link;
			}
		}

		void OpenFile(string path)
		{
			filePath = path; // @TODO more than 1 file

			try
			{
				file = File.ReadAllBytes(filePath);

				isFileLoaded = true;
				fileName = Path.GetFileName(filePath);
				fileLines = (int)Math.Ceiling((decimal)file.Length / 16);

				if (file.Length > 52 // @TODO sizeof Elf32_Ehdr
					&& file[0] == 0x7F
					&& file[1] == 'E'
					&& file[2] == 'L'
					&& file[3] == 'F')
				{

					if (file[4] == 1)
					{
						byte[] buffer = new byte[52]; // @TODO sizeof Elf32_Ehdr
						Array.Copy(file, 0,
							buffer, 0, 52);

						GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
						elfHeader = (ELFheader32)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(ELFheader32));
						handle.Free();

						// @TODO check if elfHeader.sizeProgramHeaderTableEntry == 32?
						// @TODO check if elfHeader.sizeSectionHeaderTableEntry == 40?

						Elf32_Phdr[] programHeaders;
						programHeaders = new Elf32_Phdr[elfHeader.numProgramTableEntries];

						for (int i = 0; i < elfHeader.numProgramTableEntries; i++)
						{
							buffer = new byte[elfHeader.sizeProgramTableEntry]; // @TODO sizeof Elf32_Ehdr
							Array.Copy(file, elfHeader.programTableOffset + (i * elfHeader.sizeProgramTableEntry),
								buffer, 0, elfHeader.sizeProgramTableEntry);

							handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
							programHeaders[i] = (Elf32_Phdr)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Elf32_Phdr));
							handle.Free();
						}

						Elf32_Shdr[] sectionHeaders;
						sectionHeaders = new Elf32_Shdr[elfHeader.numSectionTableEntries];

						for (int i = 0; i < elfHeader.numSectionTableEntries; i++)
						{
							buffer = new byte[elfHeader.sizeSectionTableEntry]; // @TODO sizeof Elf32_Ehdr
							Array.Copy(file, elfHeader.sectionTableOffset + (i * elfHeader.sizeSectionTableEntry),
								buffer, 0, elfHeader.sizeSectionTableEntry);

							handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
							sectionHeaders[i] = (Elf32_Shdr)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Elf32_Shdr));
							handle.Free();
						}

						StringBuilder s = new StringBuilder();

						s.Append(PrintFields(elfHeader));
						s.Append(Environment.NewLine);
						for (int i = 0; i < programHeaders.Length; i++)
						{
							var o = programHeaders[i];
							s.Append("[Program " + i.ToString() + "]" + Environment.NewLine);
							s.Append(PrintFields(o));
							s.Append(Environment.NewLine);
						}
						s.Append(Environment.NewLine);
						for (int i = 0; i < sectionHeaders.Length; i++)
						{
							var o = sectionHeaders[i];
							s.Append("[Section " + i.ToString() + "]" + Environment.NewLine);
							s.Append(PrintFields(o));
							s.Append(Environment.NewLine);
						}

						if (programHeaders.Length > 0 && programHeaders[0].p_filesz > 12) // @TODO Can it be in a different entry?
						{
							int magic, flags, checksum;

							magic = BitConverter.ToInt32(file, (int)programHeaders[0].p_offset);
							flags = BitConverter.ToInt32(file, (int)programHeaders[0].p_offset + 4);
							checksum = BitConverter.ToInt32(file, (int)programHeaders[0].p_offset + 8);

							if (magic == 0x1BADB002 && checksum == -(magic + flags))
							{
								isMultiboot = true;

								s.Append("[Multiboot]" + Environment.NewLine
									+ "magic:\t" + magic + "\t(0x" + magic.ToString("x8") + ")" + Environment.NewLine
									+ "flags:\t" + flags + "\t(0x" + flags.ToString("x8") + ")" + Environment.NewLine
									+ "checksum:\t" + checksum + "\t(0x" + checksum.ToString("x8") + ")" + Environment.NewLine);
							}
						}

						dataTextBox.Text = s.ToString();

						#region disassembly

						if (programHeaders.Length > 0)
						{
							uint at = programHeaders[0].p_offset;
							// @TODO different entries than [0]
							//uint max = at + programHeaders[0].p_filesz;
							uint max = at + 8 * 8;

							if (isMultiboot)
								at += 12; // @TODO Multiboot header constant

							string disassembly = "";

							OPcodes.Init();
							//List<string> list = new List<string>();

							/*foreach (string line in OPcodes.opCodes)
							{
								string[] args = line.Split('\t');

								if (args.Length > 2)
								{
									string arg = args[2];

									if (list.IndexOf(arg) == -1)
										list.Add(arg);
								}
								if (args.Length == 4)
								{
									string arg = args[3];

									if (list.IndexOf(arg) == -1)
										list.Add(arg);
								}
							}*/

							while (at < max)
							{
								string output, op;
								string[] parts;

								op = OPcodes.opCodes[file[at]];
								parts = op.Split('\t');

								/*if (parts.Length > 2)
								{
									string arg = parts[parts.Length - 1];

									if (list.IndexOf(arg) == -1)
										list.Add(arg);

									arg = parts[parts.Length - 2];

									if (list.IndexOf(arg) == -1)
										list.Add(arg);
								}*/

								output = parts[0] + '\t' + parts[1];

								if (parts.Length > 2)
								{
									/*string arg = parts[2];

									if (list.IndexOf(arg) == -1)
										list.Add(arg);*/

									string operand = parts[2];

									if (Array.IndexOf(OPcodes.literals, operand) != -1)
									{
										output += "\t" + operand;
									}
									else
									{
										switch (operand)
										{
											case "Ib":
											case "I0":
											case "Jb":
												at++;
												output += "\t0x" + file[at].ToString("x2");
												break;
											case "Iv":
											case "Iw":
											case "Jv":
												{
													at++;
													uint word = BitConverter.ToUInt32(file, (int)at);
													output += "\t0x" + word.ToString("x8");
													at += 3;
												}
												break;
										}
										/* @TODO
Eb
Gb
Ev
Gv
Ew
Sw
M
Ap
Ob
Ov
Mp */
									}
								}
								if (parts.Length == 4)
								{
									/*string arg = parts[2];

									if (list.IndexOf(arg) == -1)
										list.Add(arg);*/

									string operand = parts[3];

									if (Array.IndexOf(OPcodes.literals, operand) != -1)
									{
										output += "\t" + operand;
									}
									else
									{
										// @TODO fix duplicate code
										switch (operand)
										{
											case "Ib":
											case "I0":
											case "Jb":
												at++;
												output += "\t0x" + file[at].ToString("x2");
												break;
											case "Iv":
											case "Iw":
											case "Jv":
												{
													at++;
													uint word = BitConverter.ToUInt32(file, (int)at);
													output += "\t0x" + word.ToString("x8");
													at += 3;
												}
												break;
										}
									}
								}

								disassembly += output + Environment.NewLine;

								at++;
							}

							assemblyTextBox.Text = disassembly;

							//assemblyTextBox.Text += Environment.NewLine + Environment.NewLine;
							/*foreach (string str in list)
							{
								assemblyTextBox.Text += str + Environment.NewLine;
							}*/

						}

						#endregion

						isELFfile = true;
						viewDataRadio.Checked = true;
					}
					else
					{
						// @TODO 64bit ELF
						MessageBox.Show("64bit ELF not implemented yet.");
					}
				}
				else
				{
					isELFfile = false;
					viewTextRadio.Checked = true;
				}

				textBox.Text = System.Text.Encoding.Default.GetString(file);

				dragFileHereLabel.Visible = false;

				viewSwitchPanel.Visible = true;
				viewAssemblyRadio.Visible = isELFfile;
				//viewDataRadio.Visible = isELFfile;

				MainForm_Resize(null, null);
				scrollBarV.Value = 0;

				if (fileName.EndsWith(".foxlangproj"))
				{
					compileButton.Visible = true;
					// @TODO auto-compile for now
					compileButton_Click(null, null);
				}
				else
				{
					compileButton.Visible = false;
				}

				this.Text = fileName + " – " + PRODUCT_NAME;
			}
			catch (Exception ex)
			{
				// @TODO show non-intrusively inside app
				MessageBox.Show(this, "Something went wrong!" + Environment.NewLine + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				file = null;
			}
		}

		private void MainForm_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length > 0)
				{
					OpenFile(files[0]);
				}
			}
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			//Size sizeFont = TextRenderer.MeasureText("M", font);

			maxLines = (int)Math.Ceiling(mainBox.Height / font.GetHeight());

			if (isFileLoaded && fileLines > maxLines)
			{
				scrollBarV.Maximum = fileLines;
				scrollBarV.LargeChange = maxLines; // @TODO shouldn't scroll past semi-visible lines
				scrollBarV.Enabled = true;
			} else
			{
				scrollBarV.Enabled = false;
			}
		}

		private void scrollBarV_ValueChanged(object sender, EventArgs e)
		{
			mainBox.Refresh();
		}

		private void mainBox_Paint(object sender, PaintEventArgs e)
		{
			if (isFileLoaded)
			{
				if (viewHexRadio.Checked)
					DrawHex(e);
			}
		}

		private void viewHexRadio_CheckedChanged(object sender, EventArgs e)
		{
			mainBox.Visible = viewHexRadio.Checked;
			scrollBarV.Visible = viewHexRadio.Checked;
			if (viewHexRadio.Checked)
				mainBox.Refresh();
		}

		private void viewDataRadio_CheckedChanged(object sender, EventArgs e)
		{
			dataTextBox.Visible = viewDataRadio.Checked;
		}

		void DrawHex(PaintEventArgs e)
		{
			float lineHeight = font.GetHeight();

			int i, start, max;

			if (scrollBarV.Enabled)
			{
				start = scrollBarV.Value;
				max = fileLines - start;
				if (max > maxLines)
					max = maxLines;
			}
			else
			{
				start = 0;
				max = fileLines;
			}



			for (i = 0; i < max; i++)
			{
				int ii = start + i;
				e.Graphics.DrawString((ii * 16).ToString("x8"), font, Brushes.Gray, 0, i * lineHeight);

				if (ii == fileLines - 1) // last line
				{
					e.Graphics.DrawString(ByteToHex.ByteArrayToHexViaLookup32(file, (ii * 16)), font, Brushes.Black, 100, // @TODO non-fixed offset
						i * lineHeight);

					e.Graphics.DrawString(ByteArrayToASCIIString(file, (ii * 16)), font, Brushes.Black, 500, // @TODO non-fixed offset
						i * lineHeight);
				}
				else
				{
					e.Graphics.DrawString(ByteToHex.ByteArrayToHexViaLookup32(file, (ii * 16), 16), font, Brushes.Black, 100, // @TODO non-fixed offset
						i * lineHeight);

					e.Graphics.DrawString(ByteArrayToASCIIString(file, (ii * 16), 16), font, Brushes.Black, 500, // @TODO non-fixed offset
						i * lineHeight);
				}
			}
		}

		private void viewAssemblyRadio_CheckedChanged(object sender, EventArgs e)
		{
			assemblyTextBox.Visible = viewAssemblyRadio.Checked;
		}

		private void viewTextRadio_CheckedChanged(object sender, EventArgs e)
		{
			textBox.Visible = viewTextRadio.Checked;
		}

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F5 && compileButton.Visible)
			{
				compileButton_Click(null, null);
				e.Handled = true;
			}
		}

		private void compileButton_Click(object sender, EventArgs e)
		{
			bool success;
			FoxlangCompiler compiler = new FoxlangCompiler();

			success = compiler.CompileProject(filePath);

			StringBuilder sb = new StringBuilder();

			if (success)
				sb.Append("Success!");
			else
				sb.Append("Error!");

			sb.Append(Environment.NewLine);

			foreach (FoxlangCompiler.OutputMessage msg in compiler.outputMessages)
			{
				sb.Append("[" + msg.type.ToString() + "] \t" + msg.token.token + "\t" + msg.message + "\t(" + msg.filename.Substring(Path.GetDirectoryName(filePath).Length + 1) + ")[line " + msg.token.line + ", col " + msg.token.col + "]");
				sb.Append(Environment.NewLine);
			}

			/*sb.Append(Environment.NewLine);
			sb.Append(Environment.NewLine);

			foreach (FoxlangCompiler.Token token in compiler.tokens)
			{
				sb.Append(token.token + "\t[line: " + token.line + ", col: " + token.col + "]");
				sb.Append(Environment.NewLine);
			}*/

			dataTextBox.Text = sb.ToString();

			viewDataRadio.Checked = true;
		}

		string ByteArrayToASCIIString(byte[] array, int start, int length = 0)
		{
			StringBuilder builder;
			int max;

			if (length == 0)
			{
				max = array.Length;
				builder = new StringBuilder(max - start);
			}
			else
			{
				builder = new StringBuilder(length);
				max = start + length;
			}

			for (int i = start; i < max; i++)
			{
				if (array[i] >= 32 && array[i] < 127)
					builder.Append(Convert.ToChar(array[i]));
				else
					builder.Append('.');
			}

			return builder.ToString();
		}

		string PrintFields(object o)
		{
			StringBuilder s = new StringBuilder();

			foreach (var field in o.GetType().GetFields())
			{
				string str;

				str = field.GetValue(o).ToString();

				string hex = HexGetValAbstract(field.GetValue(o), field.FieldType);
				if (hex.Length > 0)
					str += "\t(0x" + HexGetValAbstract(field.GetValue(o), field.FieldType) + ")";

				if (field.FieldType.IsEnum)
				{
					object a = Convert.ChangeType(field.GetValue(o), Enum.GetUnderlyingType(field.FieldType));
					str += "\t" + a.ToString() + "\t(0x" + HexGetValAbstract(a, Enum.GetUnderlyingType(field.FieldType)) + ")";
					//str += "\t(0x" + ((Byte)field.GetValue(o)).ToString("x2") + ")";
				}

				s.Append(field.Name + ":\t" + str);

				if (field.FieldType.IsValueType && field.FieldType.IsEnum == false && field.FieldType.IsPrimitive == false)
				{
					s.Append(":" + Environment.NewLine);
					s.Append(PrintFields(field.GetValue(o)));
					s.Append(Environment.NewLine);
				}
				else
				{
					s.Append(Environment.NewLine);
				}
			}

			return s.ToString();
		}

		string HexGetValAbstract(object o, Type type)
		{
			string str = "";

			if (type == typeof(Byte))
			{
				str = ((Byte)o).ToString("x2");
			}
			else if (type == typeof(UInt16))
			{
				str = ((UInt16)o).ToString("x4");
			}
			else if (type == typeof(UInt32))
			{
				str = ((UInt32)o).ToString("x8");
			}

			return str;
		}
	}
}
