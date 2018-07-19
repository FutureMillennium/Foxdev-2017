using Foxlang;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Uide
{
	public partial class MainForm : Form
	{
		const string PRODUCT_NAME = "Foxdev by Zdeněk Gromnica";
		const int DEFAULT_MARGIN = 30;

		const int tabSize = 4; // @warning must not be < 1
		const int tWrapTabSize = 2;

		const int paddingWidth = 5;

		enum CompilerType { Foxlang, ZM01 }
		enum CompilerSubtype { Foxasm, FoxBC, Foxlang, ZM01Asm }

		bool isFileLoaded = false,
			isELFfile,
			isCompilable = false;
		string filePath,
			fileName,
			fileText;
		string[] lines;
		int[] wrappedLines;
		int[] tabCountsLine;
		int[] lenWrappedLine;

		byte[] file;
		ELFheader32 elfHeader;
		int maxVisibleLines = 0,
			hexLines = 0,
			maxCharsPerLine;
		bool isMultiboot = false;

		int deltaScrollWheel,
			linesScrollWheel;

		int leftMargin = 0;
		int charWidth;
		int lineHeight;

		int xCursor = -1,
			yCursor = -1,
			yLineCursor = -1;

		Font font = new Font("Consolas", 14, GraphicsUnit.Pixel);
		SolidBrush outsideDocumentBrush = new SolidBrush(Color.FromArgb(0xf8, 0xf8, 0xf8));
		SolidBrush eolBrush = new SolidBrush(Color.FromArgb(0xfa, 0xfa, 0xfa));
		SolidBrush wrapBrush = new SolidBrush(Color.FromArgb(0xee, 0xee, 0xff));

		FoxlangCompiler compiler;

		int Measure(string text)
		{
			return text.Length * charWidth;
		}

		int LineLength(string line)
		{
			int tabCount = 0;

			for (int j = 0; j < line.Length; j++)
			{
				char c = line[j];
				if (c == '\t')
					tabCount++;
				else
					break;
			}

			int len = line.Length + (tabCount * (tabSize - 1));

			if (line.Length > 0 && line[line.Length - 1] == '\r')
				len--;

			return len;
		}

		public MainForm(string[] args)
		{
			InitializeComponent();

			deltaScrollWheel = SystemInformation.MouseWheelScrollDelta;
			linesScrollWheel = SystemInformation.MouseWheelScrollLines;

			mainBox.MouseWheel += MainBox_MouseWheel;

			this.Text = PRODUCT_NAME;
			this.Icon = Uide.Properties.Resources.Foxdev;

			lineHeight = (int)Math.Ceiling(font.GetHeight());
			MainForm_Resize(null, null);

			if (args.Length > 0)
				OpenFile(args[0]);
		}

		void UpdateDisplay()
		{
			if (viewTextRadio.Checked || viewHexRadio.Checked)
			{
				mainBox.Refresh();
				MainForm_Resize(null, null);
			}
		}

		private void MainBox_MouseWheel(object sender, MouseEventArgs e)
		{
			int delta = e.Delta / deltaScrollWheel * linesScrollWheel * -1;
			if (delta < 0 && scrollBarV.Value + delta < 0)
			{
				scrollBarV.Value = 0;
			}
			else if (delta > 0 && scrollBarV.Value + delta > scrollBarV.Maximum - scrollBarV.LargeChange + 1)
			{
				scrollBarV.Value = scrollBarV.Maximum - scrollBarV.LargeChange + 1;
			}
			else
			{
				scrollBarV.Value += delta;
			}
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

			file = File.ReadAllBytes(filePath);

			isFileLoaded = true;
			fileName = Path.GetFileName(filePath);
			hexLines = (int)Math.Ceiling((decimal)file.Length / 16);


			fileText = System.Text.Encoding.Default.GetString(file);
			lines = fileText.Split('\n');

			leftMargin = 0;

			/*Graphics graphics;
			graphics = this.CreateGraphics();*/

			//charWidth = (int)Math.Ceiling(graphics.MeasureString("W", font).Width); // @TODO returns too big for some reason?
			charWidth = 8;

			leftMargin = (lines.Length.ToString().Length * charWidth) + (paddingWidth * 2) + 4; // @TODO corner case when lineCount > lines.Length
			if (leftMargin < DEFAULT_MARGIN)
				leftMargin = DEFAULT_MARGIN;

			//graphics.Dispose();


			if (file.Length > 52 // @TODO sizeof Elf32_Ehdr
				&& file[0] == 0x7F
				&& file[1] == 'E'
				&& file[2] == 'L'
				&& file[3] == 'F')
			{
				#region ELF file parsing
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
						assemblyTextBox.Text = Disassemble(programHeaders[0].p_offset, 8 * 8);
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
				#endregion

				viewAssemblyRadio.Visible = true;
			}
			else
			{
				isELFfile = false;

				if (fileName.EndsWith(".com"))
				{
					assemblyTextBox.Text = Disassemble(0, (uint)file.Length - 2); // @TODO @hack
					viewAssemblyRadio.Checked = true;
					viewAssemblyRadio.Visible = true;
				}
				else
				{
					viewTextRadio.Checked = true;
					viewAssemblyRadio.Visible = false;
				}

			}

			noDocPanel.Visible = false;

			viewSwitchPanel.Visible = true;

			scrollBarV.Value = 0;
			UpdateDisplay();

			if (fileName.EndsWith(".foxasm") || fileName.EndsWith(".foxlang") || fileName.EndsWith(".foxlangproj") || fileName.EndsWith(".foxbc")
				|| fileName.EndsWith(".zmasm"))
			{
				isCompilable = true;
			}
			else
				isCompilable = false;

			if (isCompilable)
			{
				compileButton.Visible = true;
#if DEBUG
				//compileButton_Click(null, null);
#endif
			}
			else
			{
				compileButton.Visible = false;
			}

			if (isCompilable || isELFfile)
			{
				viewDataRadio.Visible = true;
			}
			else
				viewDataRadio.Visible = false;

			this.Text = fileName + " – " + PRODUCT_NAME + " – " + filePath;
			/*}
			catch (Exception ex)
			{
				// @TODO show non-intrusively inside app
				MessageBox.Show(this, "Something went wrong!" + Environment.NewLine + Environment.NewLine + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				file = null;
			}*/
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

		string Disassemble(uint at, uint max)
		{
			// @TODO different entries than [0]
			//uint max = at + programHeaders[0].p_filesz;
			max += at;

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

			return disassembly;

			//assemblyTextBox.Text += Environment.NewLine + Environment.NewLine;
			/*foreach (string str in list)
			{
				assemblyTextBox.Text += str + Environment.NewLine;
			}*/
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			//Size sizeFont = TextRenderer.MeasureText("M", font);

			if (lineHeight == 0)
				return;

			maxVisibleLines = (int)Math.Ceiling(mainBox.Height / (double)lineHeight) - 1;

			wrappedLines = new int[maxVisibleLines + 2];
			tabCountsLine = new int[maxVisibleLines + 2];
			lenWrappedLine = new int[maxVisibleLines + 2];

			if (maxVisibleLines <= 0)
				return;

			if (isFileLoaded)
			{
				if (viewTextRadio.Checked)
				{
					maxCharsPerLine = (int)Math.Floor((mainBox.Width - leftMargin) / (double)charWidth) - 1;

					//lineCount = lines.Length;

					/*for (int i = 0; i < lines.Length; i++)
					{
						string line = lines[i];
						int tabCount = 0;

						for (int j = 0; j < line.Length; j++)
						{
							char c = line[j];
							if (c == '\t')
								tabCount++;
							else
								break;
							// @TODO remove \r
						}

						int len = line.Length + (tabCount * (tabSize - 1));

						if (len > maxCharsPerLine)
						{
							//lineCount += (int)Math.Ceiling(len / (double)maxCharsPerLine) - 1;
							mainBox.Refresh();
							break;
						}
					}*/

					//scrollBarV.Maximum = lineCount;
					scrollBarV.Maximum = lines.Length;
					scrollBarV.LargeChange = maxVisibleLines;
				}
				else
				{
					scrollBarV.Maximum = hexLines - 1;
					scrollBarV.LargeChange = maxVisibleLines - 1;
				}

				mainBox.Refresh();
			}
			else
			{
				scrollBarV.Value = 0;
				scrollBarV.Enabled = false;
			}
		}

		private void UpdateScrollbar()
		{
			if (scrollBarV.Maximum > maxVisibleLines)
				scrollBarV.Enabled = true;
			else
				scrollBarV.Enabled = false;
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
				else if (viewTextRadio.Checked)
					DrawCode(e);
			}
		}

		private void viewAssemblyRadio_CheckedChanged(object sender, EventArgs e)
		{
			assemblyTextBox.Visible = viewAssemblyRadio.Checked;
		}

		private void viewTextRadio_CheckedChanged(object sender, EventArgs e)
		{
			mainBox.Visible = (viewHexRadio.Checked || viewTextRadio.Checked);
			scrollBarV.Visible = (viewHexRadio.Checked || viewTextRadio.Checked);
			UpdateDisplay();
		}

		private void viewHexRadio_CheckedChanged(object sender, EventArgs e)
		{
			mainBox.Visible = (viewHexRadio.Checked || viewTextRadio.Checked);
			scrollBarV.Visible = (viewHexRadio.Checked || viewTextRadio.Checked);
			UpdateDisplay();
		}

		private void viewDataRadio_CheckedChanged(object sender, EventArgs e)
		{
			dataTextBox.Visible = viewDataRadio.Checked;
		}

		void DrawHex(PaintEventArgs e)
		{
			int i, start, max;

			if (scrollBarV.Enabled)
			{
				start = scrollBarV.Value;
				max = hexLines - start;
				if (max > maxVisibleLines)
					max = maxVisibleLines;
			}
			else
			{
				start = 0;
				max = hexLines;
			}

			{
				i = 0;
				string line = "000102030405060708090A0B0C0D0E0F";
				int jMax = line.Length / 2;

				for (int j = 0; j < jMax; j++)
				{
					e.Graphics.DrawString(line.Substring(j * 2, 2), font, Brushes.Gray, 72 + j * 21, 0);
				}
			}


			for (i = 0; i < max; i++)
			{
				float top = (i + 1) * lineHeight;
				int ii = start + i;
				e.Graphics.DrawString((ii * 16).ToString("x8"), font, Brushes.Gray, 0, top);

				int length;

				if (ii == hexLines - 1)
					length = 0;
				else
					length = 16;

				string line = ByteToHex.ByteArrayToHexViaLookup32(file, (ii * 16), length);
				int jMax = line.Length / 2;

				for (int j = 0; j < jMax; j++)
				{
					e.Graphics.DrawString(line.Substring(j * 2, 2), font, Brushes.Black, 72 + j * 21, // @TODO non-fixed offset
						top);
				}

				/*e.Graphics.DrawString(line, font, Brushes.Black, 100, // @TODO non-fixed offset
					i * lineHeight);*/

				e.Graphics.DrawString(ByteArrayToASCIIString(file, (ii * 16), length), font, Brushes.Black, (16 * 21) + 77, // @TODO non-fixed offset
					top);
			}
		}

		private void errorsListBox_DoubleClick(object sender, EventArgs e)
		{
			if (errorsListBox.SelectedIndex <= 0)
				return;

			var msg = compiler.outputMessages[errorsListBox.SelectedIndex - 1];

			if (msg.token == null)
				return;

			yCursor = msg.token.line - 1;
			yLineCursor = msg.token.line - 1;
			xCursor = msg.token.col - 1;
			scrollBarV.Value = msg.token.line - 1;
			mainBox.Refresh();
		}

		private void commandLineTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Escape:
					mainBox.Focus();
					e.Handled = true;
					break;
			}
		}

		private void mainBox_MouseDown(object sender, MouseEventArgs e)
		{
			if (viewTextRadio.Checked)
			{
				if (e.X >= leftMargin)
					xCursor = (int)Math.Round((e.X - leftMargin - 1) / (double)charWidth, 0, MidpointRounding.AwayFromZero);
				else
					xCursor = 0;
				yCursor = (int)Math.Floor((e.Y - paddingWidth) / (double)lineHeight);

				if (yCursor > -1)
				{
					if (yCursor + scrollBarV.Value >= lines.Length)
					{
						yCursor = lines.Length - 1 - scrollBarV.Value;
					}

					yLineCursor = wrappedLines[yCursor] + scrollBarV.Value;

					int len;

					if (lenWrappedLine[yCursor] != -1)
					{
						int min = (tabCountsLine[yCursor] * tabSize) + tWrapTabSize;
						len = lenWrappedLine[yCursor] + min;

						if (xCursor < min)
							xCursor = min;

					} else {
						int min = (tabCountsLine[yCursor] * tabSize);
						len = LineLength(lines[yLineCursor]);

						if (xCursor < min)
							xCursor = (int)Math.Round((xCursor - 1) / (double)tabSize, 0, MidpointRounding.AwayFromZero) * tabSize;
					}

					if (xCursor > len)
						xCursor = len;

					yCursor += scrollBarV.Value;
				}

				mainBox.Refresh();
			}
		}

		private void newFileButton_Click(object sender, EventArgs e)
		{
			fileText = "";
			viewTextRadio.Checked = true;
			viewTextRadio.Visible = true;

			viewHexRadio.Visible = false;
			viewDataRadio.Visible = false;
			viewAssemblyRadio.Visible = false;
			compileButton.Visible = false;

			noDocPanel.Visible = false;

			mainBox.Focus();

			viewSwitchPanel.Visible = true;
		}

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Escape:
					errorsListBox.Visible = !errorsListBox.Visible;
					break;
				case Keys.F1:
					commandLineTextBox.Focus();
					break;
				case Keys.F5:
					if (compileButton.Visible)
					{
						compileButton_Click(null, null);
						e.Handled = true;
					}
					break;
				case Keys.W:
					if (e.Control == true)
					{
						this.Close();
					}
					break;
			}
		}

		void ZM01Compile(CompilerSubtype subtype)
		{
			ZM01.ZMAsm zmAsm = new ZM01.ZMAsm();
			bool success = zmAsm.Compile(filePath);

			StringBuilder sb = new StringBuilder();

			errorsListBox.Items.Clear();

			string resMessage;
			if (success)
				resMessage = "Success!";
			else
				resMessage = "Error!";

			errorsListBox.Items.Add(resMessage);
			sb.Append(resMessage);

			sb.Append(Environment.NewLine);

			foreach (var msg in zmAsm.outputMessages)
			{
				string message = "[" + msg.type.ToString() + "] \t";

				if (msg.token != null)
					message += msg.token.token + "\t" + msg.message + "\t(" + msg.filename.Substring(Path.GetDirectoryName(filePath).Length + 1) + ")[line " + msg.token.line + ", col " + msg.token.col + "]";
				else
					message += msg.message + "\t(" + msg.filename.Substring(Path.GetDirectoryName(filePath).Length + 1) + ")";
				errorsListBox.Items.Add(message);
				sb.Append(message);
				sb.Append(Environment.NewLine);
			}

			errorsListBox.Height = errorsListBox.ItemHeight * (errorsListBox.Items.Count + 1);
			errorsListBox.Visible = true;
			errorsListBox.Top = this.ClientSize.Height - bottomPanel.Height - errorsListBox.Height;

			// write out all tokens
			sb.Append(Environment.NewLine);
			sb.Append(Environment.NewLine);

			foreach (Compiler.Token token in zmAsm.tokens)
			{
				sb.Append(token.ToString());
				sb.Append(Environment.NewLine);
			}

			dataTextBox.Text = sb.ToString();

			viewDataRadio.Checked = true;


		}

		void FoxlangCompile(CompilerSubtype subtype)
		{
			compiler = new FoxlangCompiler();
			bool success = false;

			switch (subtype)
			{
				case CompilerSubtype.Foxasm:
					success = compiler.FoxasmCompile(filePath);
					break;
				case CompilerSubtype.FoxBC:
					success = compiler.FoxBCCompileFile(filePath);
					break;
				case CompilerSubtype.Foxlang:
					success = compiler.CompileProject(filePath);
					break;
			}

			StringBuilder sb = new StringBuilder();

			errorsListBox.Items.Clear();

			string resMessage;
			if (success)
				resMessage = "Success!";
			else
				resMessage = "Error!";

			errorsListBox.Items.Add(resMessage);
			sb.Append(resMessage);

			sb.Append(Environment.NewLine);

			foreach (Foxlang.OutputMessage msg in compiler.outputMessages)
			{
				string message = "[" + msg.type.ToString() + "] \t";

				if (msg.token != null)
					message += msg.token.token + "\t" + msg.message + "\t(" + msg.filename.Substring(Path.GetDirectoryName(filePath).Length + 1) + ")[line " + msg.token.line + ", col " + msg.token.col + "]";
				else
					message += msg.message + "\t(" + msg.filename.Substring(Path.GetDirectoryName(filePath).Length + 1) + ")";
				errorsListBox.Items.Add(message);
				sb.Append(message);
				sb.Append(Environment.NewLine);
			}

			errorsListBox.Height = errorsListBox.ItemHeight * (errorsListBox.Items.Count + 1);
			errorsListBox.Visible = true;
			errorsListBox.Top = this.ClientSize.Height - bottomPanel.Height - errorsListBox.Height;



			if (compiler.output != null)
			{
				sb.AppendLine();
				sb.AppendLine("[Binary output]");
				sb.AppendLine(compiler.output);
			}


			sb.AppendLine();
			sb.AppendLine("[Bytecode output]");

			sb.Append(compiler.EchoBytecode());


			// write out all tokens
			/*sb.Append(Environment.NewLine);
			sb.Append(Environment.NewLine);

			foreach (FoxlangCompiler.Token token in compiler.tokens)
			{
				sb.Append(token.token + "\t[line: " + token.line + ", col: " + token.col + "]");
				sb.Append(Environment.NewLine);
			}*/

			dataTextBox.Text = sb.ToString();

			//viewDataRadio.Checked = true;
		}

		private void compileButton_Click(object sender, EventArgs e)
		{
			string[] filenameParts = filePath.Split('.');

			if (filenameParts.Length == 1)
			{
				return; // @TODO error
			}

			CompilerType compilerType;
			CompilerSubtype subtype;

			switch (filenameParts[filenameParts.Length - 1])
			{
				case "zmasm":
					compilerType = CompilerType.ZM01;
					subtype = CompilerSubtype.ZM01Asm;
					break;
				case "foxasm":
					compilerType = CompilerType.Foxlang;
					subtype = CompilerSubtype.Foxasm;
					break;
				case "foxbc":
					compilerType = CompilerType.Foxlang;
					subtype = CompilerSubtype.FoxBC;
					break;
				/*case "foxlang":
				case "foxlangproj":*/
				default:
					compilerType = CompilerType.Foxlang;
					subtype = CompilerSubtype.Foxlang;
					break;
			}

			switch (compilerType)
			{
				case CompilerType.Foxlang:
					FoxlangCompile(subtype);
					break;
				case CompilerType.ZM01:
					ZM01Compile(subtype);
					break;
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

		void DrawCode(PaintEventArgs e)
		{
			int max, start = scrollBarV.Value;

			e.Graphics.FillRectangle(outsideDocumentBrush, 0, 0, leftMargin, mainBox.Height);

			max = maxVisibleLines + 1;

			if (max + start > lines.Length)
			{
				max = lines.Length - start;
			}

			int i = 0;
			int iAdj = 0;

			if (start == 0)
			{
				e.Graphics.FillRectangle(outsideDocumentBrush, 0, 0, mainBox.Width, paddingWidth);
			}
			else
			{
				i = -1;
				iAdj = -1;
			}

			int prevTabC = 0;
			int adjBy = 0;

			for (; i < max; i++)
			{
				float top = iAdj * lineHeight + paddingWidth;
				
				string lineNum = (i + start + 1).ToString();

				if (iAdj > -1)
				{
					wrappedLines[iAdj] = i;
					lenWrappedLine[iAdj] = -1;
				}

				e.Graphics.DrawString(lineNum, font, Brushes.Gray, leftMargin - e.Graphics.MeasureString(lineNum, font).Width - paddingWidth, top);
				//e.Graphics.DrawString(lines[i + start], font, Brushes.Black, leftMargin + paddingWidth, top);

				int j = 0,
					jLast = 0;
				int offset = 0;
				int oTabOffset = 0;
				int tabC = 0;
				string line = lines[i + start];
				int lenLine = line.Length;
				bool wrappedLine = false;

				for (; j < line.Length; j++)
				{
					char c = line[j];

					if (c == '\t')
					{
						int l = offset * charWidth + leftMargin;
						e.Graphics.DrawLine(Pens.LightGray, l, top,
							l, top + lineHeight);

						offset += tabSize;
						oTabOffset += tabSize;

						tabC++;
						continue;
					}
					else if (c == '\r')
					{
						lenLine--;
						break;
					}

					if (offset >= maxCharsPerLine)
					{
						float l = offset * charWidth + leftMargin;
						e.Graphics.FillRectangle(wrapBrush, l, top,
							mainBox.Width - l, lineHeight); // end of line wrap grey

						if (iAdj > -1 && iAdj < lenWrappedLine.Length)
						{
							if (wrappedLine)
							{
								lenWrappedLine[iAdj] = j - jLast;
							}
							else
							{
								tabCountsLine[iAdj] = tabC;
							}
						}
						jLast = j;

						offset = oTabOffset + tWrapTabSize;
						top += lineHeight;
						iAdj++;
						adjBy++;

						if (iAdj < wrappedLines.Length) {
							wrappedLines[iAdj] = i;
						}

						e.Graphics.FillRectangle(wrapBrush, 0, top,
							offset * charWidth + leftMargin, lineHeight); // beginning of line wrap grey

						wrappedLine = true;
					}

					e.Graphics.DrawString(c.ToString(), font, Brushes.Black, leftMargin + (charWidth * offset) - 2, top); // @TODO why 2?

					offset++;
				}

				if (wrappedLine && iAdj > 0 && iAdj < lenWrappedLine.Length)
				{
					lenWrappedLine[iAdj] = j - jLast;
				}

				float w = offset * charWidth + leftMargin + 2;
				e.Graphics.FillRectangle(eolBrush, w, top, mainBox.Width - w, lineHeight); // end of line grey

				if (lenLine == 0 && prevTabC > 0)
				{
					tabC = prevTabC;
					for (j = 0; j < prevTabC; j++)
					{
						offset = j * tabSize;

						int l = offset * charWidth + leftMargin; // @TODO @cleanup @dupl
						e.Graphics.DrawLine(Pens.LightGray, l, top,
							l, top + lineHeight);
					}
				}

				if (iAdj > -1 && iAdj < tabCountsLine.Length)
					tabCountsLine[iAdj] = tabC;
				prevTabC = tabC;
				iAdj++;
				if (iAdj > (maxVisibleLines + 1))
					break;
			}

			// end of file grey:
			if (i + start >= lines.Length)
			{
				e.Graphics.FillRectangle(outsideDocumentBrush, 0, iAdj * lineHeight + paddingWidth, mainBox.Width, mainBox.Height);
			}

			if (xCursor > -1 && yCursor > -1 && yLineCursor >= start - 1 && yLineCursor < start + i)
			{
				e.Graphics.FillRectangle(Brushes.Red, leftMargin + xCursor * charWidth, (yCursor - start) * lineHeight + paddingWidth,
					2, lineHeight);
			}

			int newMax = lines.Length + adjBy;
			if (scrollBarV.Value + scrollBarV.LargeChange < newMax)
			{
				scrollBarV.Maximum = newMax;
				UpdateScrollbar();
			}
		}
	}
}
