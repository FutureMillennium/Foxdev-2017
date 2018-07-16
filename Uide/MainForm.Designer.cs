namespace Uide
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.mainBox = new Uide.SelectablePictureBox();
			this.scrollBarV = new System.Windows.Forms.VScrollBar();
			this.viewSwitchPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.compileButton = new System.Windows.Forms.Button();
			this.viewDataRadio = new System.Windows.Forms.RadioButton();
			this.viewAssemblyRadio = new System.Windows.Forms.RadioButton();
			this.viewHexRadio = new System.Windows.Forms.RadioButton();
			this.viewTextRadio = new System.Windows.Forms.RadioButton();
			this.dataTextBox = new System.Windows.Forms.TextBox();
			this.assemblyTextBox = new System.Windows.Forms.TextBox();
			this.noDocPanel = new System.Windows.Forms.Panel();
			this.newFileButton = new System.Windows.Forms.Button();
			this.dragFileHereLabel = new System.Windows.Forms.Label();
			this.bottomPanel = new System.Windows.Forms.TableLayoutPanel();
			this.commandLineTextBox = new System.Windows.Forms.TextBox();
			this.errorsListBox = new System.Windows.Forms.ListBox();
			((System.ComponentModel.ISupportInitialize)(this.mainBox)).BeginInit();
			this.viewSwitchPanel.SuspendLayout();
			this.noDocPanel.SuspendLayout();
			this.bottomPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainBox
			// 
			this.mainBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mainBox.BackColor = System.Drawing.SystemColors.Window;
			this.mainBox.Location = new System.Drawing.Point(0, 0);
			this.mainBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.mainBox.Name = "mainBox";
			this.mainBox.Size = new System.Drawing.Size(1103, 634);
			this.mainBox.TabIndex = 0;
			this.mainBox.TabStop = false;
			this.mainBox.Visible = false;
			this.mainBox.Paint += new System.Windows.Forms.PaintEventHandler(this.mainBox_Paint);
			this.mainBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mainBox_MouseDown);
			// 
			// scrollBarV
			// 
			this.scrollBarV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.scrollBarV.Enabled = false;
			this.scrollBarV.Location = new System.Drawing.Point(1103, 0);
			this.scrollBarV.Name = "scrollBarV";
			this.scrollBarV.Size = new System.Drawing.Size(36, 635);
			this.scrollBarV.TabIndex = 1;
			this.scrollBarV.Visible = false;
			this.scrollBarV.ValueChanged += new System.EventHandler(this.scrollBarV_ValueChanged);
			// 
			// viewSwitchPanel
			// 
			this.viewSwitchPanel.AutoSize = true;
			this.viewSwitchPanel.Controls.Add(this.compileButton);
			this.viewSwitchPanel.Controls.Add(this.viewDataRadio);
			this.viewSwitchPanel.Controls.Add(this.viewAssemblyRadio);
			this.viewSwitchPanel.Controls.Add(this.viewHexRadio);
			this.viewSwitchPanel.Controls.Add(this.viewTextRadio);
			this.viewSwitchPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.viewSwitchPanel.Location = new System.Drawing.Point(721, 0);
			this.viewSwitchPanel.Margin = new System.Windows.Forms.Padding(0);
			this.viewSwitchPanel.Name = "viewSwitchPanel";
			this.viewSwitchPanel.Size = new System.Drawing.Size(402, 35);
			this.viewSwitchPanel.TabIndex = 4;
			this.viewSwitchPanel.Visible = false;
			this.viewSwitchPanel.WrapContents = false;
			// 
			// compileButton
			// 
			this.compileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.compileButton.Location = new System.Drawing.Point(280, 0);
			this.compileButton.Margin = new System.Windows.Forms.Padding(15, 0, 0, 0);
			this.compileButton.Name = "compileButton";
			this.compileButton.Size = new System.Drawing.Size(122, 35);
			this.compileButton.TabIndex = 10;
			this.compileButton.Text = "Compile";
			this.compileButton.UseVisualStyleBackColor = true;
			this.compileButton.Click += new System.EventHandler(this.compileButton_Click);
			// 
			// viewDataRadio
			// 
			this.viewDataRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewDataRadio.AutoSize = true;
			this.viewDataRadio.Location = new System.Drawing.Point(205, 0);
			this.viewDataRadio.Margin = new System.Windows.Forms.Padding(0);
			this.viewDataRadio.MinimumSize = new System.Drawing.Size(60, 35);
			this.viewDataRadio.Name = "viewDataRadio";
			this.viewDataRadio.Size = new System.Drawing.Size(60, 35);
			this.viewDataRadio.TabIndex = 6;
			this.viewDataRadio.Text = "data";
			this.viewDataRadio.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.viewDataRadio.UseVisualStyleBackColor = true;
			this.viewDataRadio.CheckedChanged += new System.EventHandler(this.viewDataRadio_CheckedChanged);
			// 
			// viewAssemblyRadio
			// 
			this.viewAssemblyRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewAssemblyRadio.AutoSize = true;
			this.viewAssemblyRadio.Location = new System.Drawing.Point(120, 0);
			this.viewAssemblyRadio.Margin = new System.Windows.Forms.Padding(0);
			this.viewAssemblyRadio.MinimumSize = new System.Drawing.Size(60, 35);
			this.viewAssemblyRadio.Name = "viewAssemblyRadio";
			this.viewAssemblyRadio.Size = new System.Drawing.Size(85, 35);
			this.viewAssemblyRadio.TabIndex = 5;
			this.viewAssemblyRadio.Text = "assembly";
			this.viewAssemblyRadio.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.viewAssemblyRadio.UseVisualStyleBackColor = true;
			this.viewAssemblyRadio.CheckedChanged += new System.EventHandler(this.viewAssemblyRadio_CheckedChanged);
			// 
			// viewHexRadio
			// 
			this.viewHexRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewHexRadio.AutoSize = true;
			this.viewHexRadio.Location = new System.Drawing.Point(60, 0);
			this.viewHexRadio.Margin = new System.Windows.Forms.Padding(0);
			this.viewHexRadio.MinimumSize = new System.Drawing.Size(60, 35);
			this.viewHexRadio.Name = "viewHexRadio";
			this.viewHexRadio.Size = new System.Drawing.Size(60, 35);
			this.viewHexRadio.TabIndex = 4;
			this.viewHexRadio.Text = "hex";
			this.viewHexRadio.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.viewHexRadio.UseVisualStyleBackColor = true;
			this.viewHexRadio.CheckedChanged += new System.EventHandler(this.viewHexRadio_CheckedChanged);
			// 
			// viewTextRadio
			// 
			this.viewTextRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewTextRadio.AutoSize = true;
			this.viewTextRadio.Location = new System.Drawing.Point(0, 0);
			this.viewTextRadio.Margin = new System.Windows.Forms.Padding(0);
			this.viewTextRadio.MinimumSize = new System.Drawing.Size(60, 35);
			this.viewTextRadio.Name = "viewTextRadio";
			this.viewTextRadio.Size = new System.Drawing.Size(60, 35);
			this.viewTextRadio.TabIndex = 7;
			this.viewTextRadio.Text = "text";
			this.viewTextRadio.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.viewTextRadio.UseVisualStyleBackColor = true;
			this.viewTextRadio.CheckedChanged += new System.EventHandler(this.viewTextRadio_CheckedChanged);
			// 
			// dataTextBox
			// 
			this.dataTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataTextBox.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
			this.dataTextBox.Location = new System.Drawing.Point(0, 0);
			this.dataTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.dataTextBox.Multiline = true;
			this.dataTextBox.Name = "dataTextBox";
			this.dataTextBox.ReadOnly = true;
			this.dataTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.dataTextBox.Size = new System.Drawing.Size(1139, 634);
			this.dataTextBox.TabIndex = 5;
			this.dataTextBox.Visible = false;
			// 
			// assemblyTextBox
			// 
			this.assemblyTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.assemblyTextBox.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.assemblyTextBox.Location = new System.Drawing.Point(0, 0);
			this.assemblyTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.assemblyTextBox.Multiline = true;
			this.assemblyTextBox.Name = "assemblyTextBox";
			this.assemblyTextBox.ReadOnly = true;
			this.assemblyTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.assemblyTextBox.Size = new System.Drawing.Size(1139, 635);
			this.assemblyTextBox.TabIndex = 7;
			this.assemblyTextBox.Visible = false;
			// 
			// noDocPanel
			// 
			this.noDocPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.noDocPanel.Controls.Add(this.newFileButton);
			this.noDocPanel.Controls.Add(this.dragFileHereLabel);
			this.noDocPanel.Location = new System.Drawing.Point(459, 265);
			this.noDocPanel.Name = "noDocPanel";
			this.noDocPanel.Size = new System.Drawing.Size(222, 141);
			this.noDocPanel.TabIndex = 10;
			// 
			// newFileButton
			// 
			this.newFileButton.Location = new System.Drawing.Point(4, 87);
			this.newFileButton.Name = "newFileButton";
			this.newFileButton.Size = new System.Drawing.Size(214, 50);
			this.newFileButton.TabIndex = 8;
			this.newFileButton.Text = "Create new file [Ctrl+N]";
			this.newFileButton.UseVisualStyleBackColor = true;
			this.newFileButton.Click += new System.EventHandler(this.newFileButton_Click);
			// 
			// dragFileHereLabel
			// 
			this.dragFileHereLabel.Location = new System.Drawing.Point(3, 0);
			this.dragFileHereLabel.Name = "dragFileHereLabel";
			this.dragFileHereLabel.Size = new System.Drawing.Size(215, 84);
			this.dragFileHereLabel.TabIndex = 7;
			this.dragFileHereLabel.Text = "Drag a file here\r\n\r\nor\r\n\r\n";
			this.dragFileHereLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// bottomPanel
			// 
			this.bottomPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.bottomPanel.ColumnCount = 2;
			this.bottomPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.bottomPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.bottomPanel.Controls.Add(this.commandLineTextBox, 0, 0);
			this.bottomPanel.Controls.Add(this.viewSwitchPanel, 1, 0);
			this.bottomPanel.Location = new System.Drawing.Point(0, 635);
			this.bottomPanel.Name = "bottomPanel";
			this.bottomPanel.RowCount = 1;
			this.bottomPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.bottomPanel.Size = new System.Drawing.Size(1123, 35);
			this.bottomPanel.TabIndex = 11;
			// 
			// commandLineTextBox
			// 
			this.commandLineTextBox.AcceptsReturn = true;
			this.commandLineTextBox.AcceptsTab = true;
			this.commandLineTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.commandLineTextBox.Location = new System.Drawing.Point(3, 3);
			this.commandLineTextBox.Name = "commandLineTextBox";
			this.commandLineTextBox.Size = new System.Drawing.Size(715, 29);
			this.commandLineTextBox.TabIndex = 13;
			this.commandLineTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.commandLineTextBox_KeyDown);
			// 
			// errorsListBox
			// 
			this.errorsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.errorsListBox.FormattingEnabled = true;
			this.errorsListBox.ItemHeight = 21;
			this.errorsListBox.Location = new System.Drawing.Point(0, 421);
			this.errorsListBox.Name = "errorsListBox";
			this.errorsListBox.Size = new System.Drawing.Size(1139, 214);
			this.errorsListBox.TabIndex = 12;
			this.errorsListBox.Visible = false;
			this.errorsListBox.DoubleClick += new System.EventHandler(this.errorsListBox_DoubleClick);
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1140, 670);
			this.Controls.Add(this.errorsListBox);
			this.Controls.Add(this.scrollBarV);
			this.Controls.Add(this.mainBox);
			this.Controls.Add(this.bottomPanel);
			this.Controls.Add(this.noDocPanel);
			this.Controls.Add(this.dataTextBox);
			this.Controls.Add(this.assemblyTextBox);
			this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.KeyPreview = true;
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "MainForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
			this.Resize += new System.EventHandler(this.MainForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.mainBox)).EndInit();
			this.viewSwitchPanel.ResumeLayout(false);
			this.viewSwitchPanel.PerformLayout();
			this.noDocPanel.ResumeLayout(false);
			this.bottomPanel.ResumeLayout(false);
			this.bottomPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.VScrollBar scrollBarV;
		private System.Windows.Forms.RadioButton viewAssemblyRadio;
		private System.Windows.Forms.RadioButton viewHexRadio;
		private System.Windows.Forms.RadioButton viewDataRadio;
		private System.Windows.Forms.TextBox dataTextBox;
		private System.Windows.Forms.TextBox assemblyTextBox;
		private System.Windows.Forms.RadioButton viewTextRadio;
		private System.Windows.Forms.Button compileButton;
		private System.Windows.Forms.Panel noDocPanel;
		private System.Windows.Forms.Button newFileButton;
		private System.Windows.Forms.Label dragFileHereLabel;
		private System.Windows.Forms.FlowLayoutPanel viewSwitchPanel;
		private System.Windows.Forms.TableLayoutPanel bottomPanel;
		private System.Windows.Forms.TextBox commandLineTextBox;
		private SelectablePictureBox mainBox;
		private System.Windows.Forms.ListBox errorsListBox;
	}
}