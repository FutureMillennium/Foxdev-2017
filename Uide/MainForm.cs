using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Uide
{
	public partial class MainForm : Form
	{
		bool isFileLoaded = false, isELFfile;
		string filePath, fileName;
		byte[] file;
		ELFheader32 elfHeader;
		int maxLines = 0, fileLines = 0;

		Font font = new Font(FontFamily.GenericMonospace, 13);

		void SetDoubleBuffered(System.Windows.Forms.Control c)
		{
			//Taxes: Remote Desktop Connection and painting
			//http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
			if (System.Windows.Forms.SystemInformation.TerminalServerSession)
				return;

			System.Reflection.PropertyInfo aProp =
				  typeof(System.Windows.Forms.Control).GetProperty(
						"DoubleBuffered",
						System.Reflection.BindingFlags.NonPublic |
						System.Reflection.BindingFlags.Instance);

			aProp.SetValue(c, true, null);
		}

		


		public MainForm()
		{
			InitializeComponent();

			SetDoubleBuffered(this.mainBox);

			MainForm_Resize(null, null);
		}

		private void MainForm_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Link;
			}
		}

		private void MainForm_DragDrop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length > 0)
				{
					filePath = files[0]; // @TODO more than 1 file

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

								dataTextBox.Text = "";
								PrintFields(elfHeader);
								dataTextBox.Text += Environment.NewLine;
								for (int i = 0; i < programHeaders.Length; i++)
								{
									var o = programHeaders[i];
									dataTextBox.Text += "[Program " + i.ToString() + "]" + Environment.NewLine;
									PrintFields(o);
									dataTextBox.Text += Environment.NewLine;
								}
								dataTextBox.Text += Environment.NewLine;
								for (int i = 0; i < sectionHeaders.Length; i++)
								{
									var o = sectionHeaders[i];
									dataTextBox.Text += "[Section " + i.ToString() + "]" + Environment.NewLine;
									PrintFields(o);
									dataTextBox.Text += Environment.NewLine;
								}


								isELFfile = true;
								//viewAssemblyRadio.Checked = true;
								viewDataRadio.Checked = true;

								this.Text = fileName + " – Uide"; // @TODO name constant
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
							viewHexRadio.Checked = true;
						}

						viewSwitchPanel.Visible = isELFfile;

						MainForm_Resize(null, null);
						scrollBarV.Value = 0;
					}
					catch (Exception ex)
					{
						// @TODO show non-intrusively inside app
						MessageBox.Show(this, "Something went wrong!" + Environment.NewLine + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						file = null;
					}

					mainBox.Refresh();

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

		void PrintFields(object o)
		{
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

				dataTextBox.Text += field.Name + ":\t" + str;

				if (field.FieldType.IsValueType && field.FieldType.IsEnum == false && field.FieldType.IsPrimitive == false)
				{
					dataTextBox.Text += ":" + Environment.NewLine;
					PrintFields(field.GetValue(o));
					dataTextBox.Text += Environment.NewLine;
				}
				else
				{
					dataTextBox.Text += Environment.NewLine;
				}
			}
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
