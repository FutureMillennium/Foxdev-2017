using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Uide
{
	public partial class MainForm : Form
	{
		bool isFileLoaded = false, isELFfile;
		byte[] file;
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
					string filename = files[0]; // @TODO more than 1 file

					try
					{
						file = File.ReadAllBytes(filename);
						isFileLoaded = true;
						fileLines = (int)Math.Ceiling((decimal)file.Length / 16);

						if (file.Length > 3
						&& file[0] == 0x7F
						&& file[1] == 'E'
						&& file[2] == 'L'
						&& file[3] == 'F')
						{
							isELFfile = true;
							viewAssemblyRadio.Checked = true;
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
	}
}
