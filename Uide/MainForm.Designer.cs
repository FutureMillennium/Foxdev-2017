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
			this.viewAssemblyRadio = new System.Windows.Forms.RadioButton();
			this.viewHexRadio = new System.Windows.Forms.RadioButton();
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
			this.mainBox.Name = "mainBox";
			this.mainBox.Size = new System.Drawing.Size(724, 400);
			this.mainBox.TabIndex = 0;
			this.mainBox.TabStop = false;
			this.mainBox.Paint += new System.Windows.Forms.PaintEventHandler(this.mainBox_Paint);
			// 
			// scrollBarV
			// 
			this.scrollBarV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.scrollBarV.Enabled = false;
			this.scrollBarV.Location = new System.Drawing.Point(724, 0);
			this.scrollBarV.Name = "scrollBarV";
			this.scrollBarV.Size = new System.Drawing.Size(36, 400);
			this.scrollBarV.TabIndex = 1;
			this.scrollBarV.ValueChanged += new System.EventHandler(this.scrollBarV_ValueChanged);
			// 
			// viewSwitchPanel
			// 
			this.viewSwitchPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.viewSwitchPanel.Controls.Add(this.viewAssemblyRadio);
			this.viewSwitchPanel.Controls.Add(this.viewHexRadio);
			this.viewSwitchPanel.Location = new System.Drawing.Point(0, 400);
			this.viewSwitchPanel.Name = "viewSwitchPanel";
			this.viewSwitchPanel.Size = new System.Drawing.Size(126, 30);
			this.viewSwitchPanel.TabIndex = 4;
			this.viewSwitchPanel.Visible = false;
			// 
			// viewAssemblyRadio
			// 
			this.viewAssemblyRadio.Appearance = System.Windows.Forms.Appearance.Button;
			this.viewAssemblyRadio.AutoSize = true;
			this.viewAssemblyRadio.Location = new System.Drawing.Point(40, 0);
			this.viewAssemblyRadio.Name = "viewAssemblyRadio";
			this.viewAssemblyRadio.Size = new System.Drawing.Size(60, 23);
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
			this.viewHexRadio.Name = "viewHexRadio";
			this.viewHexRadio.Size = new System.Drawing.Size(34, 23);
			this.viewHexRadio.TabIndex = 4;
			this.viewHexRadio.TabStop = true;
			this.viewHexRadio.Text = "hex";
			this.viewHexRadio.UseVisualStyleBackColor = true;
			this.viewHexRadio.CheckedChanged += new System.EventHandler(this.viewHexRadio_CheckedChanged);
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(760, 430);
			this.Controls.Add(this.viewSwitchPanel);
			this.Controls.Add(this.mainBox);
			this.Controls.Add(this.scrollBarV);
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

		}

		#endregion

		private System.Windows.Forms.PictureBox mainBox;
		private System.Windows.Forms.VScrollBar scrollBarV;
		private System.Windows.Forms.Panel viewSwitchPanel;
		private System.Windows.Forms.RadioButton viewAssemblyRadio;
		private System.Windows.Forms.RadioButton viewHexRadio;
	}
}