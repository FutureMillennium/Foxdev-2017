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
			this.mainBox = new System.Windows.Forms.PictureBox();
			this.scrollBarV = new System.Windows.Forms.VScrollBar();
			this.viewSwitchPanel = new System.Windows.Forms.Panel();
			this.viewDataRadio = new System.Windows.Forms.RadioButton();
			this.viewAssemblyRadio = new System.Windows.Forms.RadioButton();
			this.viewHexRadio = new System.Windows.Forms.RadioButton();
			this.dataTextBox = new System.Windows.Forms.TextBox();
			this.dragFileHereLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.mainBox)).BeginInit();
			this.viewSwitchPanel.SuspendLayout();
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
			this.mainBox.Size = new System.Drawing.Size(1103, 635);
			this.mainBox.TabIndex = 0;
			this.mainBox.TabStop = false;
			this.mainBox.Visible = false;
			this.mainBox.Paint += new System.Windows.Forms.PaintEventHandler(this.mainBox_Paint);
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
			this.viewSwitchPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.viewSwitchPanel.Controls.Add(this.viewDataRadio);
			this.viewSwitchPanel.Controls.Add(this.viewAssemblyRadio);
			this.viewSwitchPanel.Controls.Add(this.viewHexRadio);
			this.viewSwitchPanel.Location = new System.Drawing.Point(0, 634);
			this.viewSwitchPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.viewSwitchPanel.Name = "viewSwitchPanel";
			this.viewSwitchPanel.Size = new System.Drawing.Size(208, 36);
			this.viewSwitchPanel.TabIndex = 4;
			this.viewSwitchPanel.Visible = false;
			// 
			// viewDataRadio
			// 
			this.viewDataRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewDataRadio.AutoSize = true;
			this.viewDataRadio.Location = new System.Drawing.Point(145, 0);
			this.viewDataRadio.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.viewDataRadio.Name = "viewDataRadio";
			this.viewDataRadio.Size = new System.Drawing.Size(50, 31);
			this.viewDataRadio.TabIndex = 6;
			this.viewDataRadio.TabStop = true;
			this.viewDataRadio.Text = "data";
			this.viewDataRadio.UseVisualStyleBackColor = true;
			this.viewDataRadio.CheckedChanged += new System.EventHandler(this.viewDataRadio_CheckedChanged);
			// 
			// viewAssemblyRadio
			// 
			this.viewAssemblyRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewAssemblyRadio.AutoSize = true;
			this.viewAssemblyRadio.Location = new System.Drawing.Point(52, 0);
			this.viewAssemblyRadio.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.viewAssemblyRadio.Name = "viewAssemblyRadio";
			this.viewAssemblyRadio.Size = new System.Drawing.Size(85, 31);
			this.viewAssemblyRadio.TabIndex = 5;
			this.viewAssemblyRadio.TabStop = true;
			this.viewAssemblyRadio.Text = "assembly";
			this.viewAssemblyRadio.UseVisualStyleBackColor = true;
			// 
			// viewHexRadio
			// 
			this.viewHexRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewHexRadio.AutoSize = true;
			this.viewHexRadio.Checked = true;
			this.viewHexRadio.Location = new System.Drawing.Point(0, 0);
			this.viewHexRadio.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.viewHexRadio.Name = "viewHexRadio";
			this.viewHexRadio.Size = new System.Drawing.Size(44, 31);
			this.viewHexRadio.TabIndex = 4;
			this.viewHexRadio.TabStop = true;
			this.viewHexRadio.Text = "hex";
			this.viewHexRadio.UseVisualStyleBackColor = true;
			this.viewHexRadio.CheckedChanged += new System.EventHandler(this.viewHexRadio_CheckedChanged);
			// 
			// dataTextBox
			// 
			this.dataTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataTextBox.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.dataTextBox.Location = new System.Drawing.Point(0, 0);
			this.dataTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.dataTextBox.Multiline = true;
			this.dataTextBox.Name = "dataTextBox";
			this.dataTextBox.ReadOnly = true;
			this.dataTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.dataTextBox.Size = new System.Drawing.Size(1139, 635);
			this.dataTextBox.TabIndex = 5;
			this.dataTextBox.Visible = false;
			// 
			// dragFileHereLabel
			// 
			this.dragFileHereLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.dragFileHereLabel.AutoSize = true;
			this.dragFileHereLabel.Location = new System.Drawing.Point(512, 324);
			this.dragFileHereLabel.Name = "dragFileHereLabel";
			this.dragFileHereLabel.Size = new System.Drawing.Size(116, 21);
			this.dragFileHereLabel.TabIndex = 6;
			this.dragFileHereLabel.Text = "Drag a file here";
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1140, 670);
			this.Controls.Add(this.dragFileHereLabel);
			this.Controls.Add(this.dataTextBox);
			this.Controls.Add(this.viewSwitchPanel);
			this.Controls.Add(this.mainBox);
			this.Controls.Add(this.scrollBarV);
			this.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "MainForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Uide";
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
			this.Resize += new System.EventHandler(this.MainForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.mainBox)).EndInit();
			this.viewSwitchPanel.ResumeLayout(false);
			this.viewSwitchPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox mainBox;
		private System.Windows.Forms.VScrollBar scrollBarV;
		private System.Windows.Forms.Panel viewSwitchPanel;
		private System.Windows.Forms.RadioButton viewAssemblyRadio;
		private System.Windows.Forms.RadioButton viewHexRadio;
		private System.Windows.Forms.RadioButton viewDataRadio;
		private System.Windows.Forms.TextBox dataTextBox;
		private System.Windows.Forms.Label dragFileHereLabel;
	}
}