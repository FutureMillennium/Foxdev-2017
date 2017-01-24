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
			((System.ComponentModel.ISupportInitialize)(this.mainBox)).BeginInit();
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
			this.mainBox.Size = new System.Drawing.Size(657, 408);
			this.mainBox.TabIndex = 0;
			this.mainBox.TabStop = false;
			this.mainBox.Paint += new System.Windows.Forms.PaintEventHandler(this.mainBox_Paint);
			// 
			// scrollBarV
			// 
			this.scrollBarV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.scrollBarV.Enabled = false;
			this.scrollBarV.Location = new System.Drawing.Point(657, 0);
			this.scrollBarV.Name = "scrollBarV";
			this.scrollBarV.Size = new System.Drawing.Size(36, 408);
			this.scrollBarV.TabIndex = 1;
			this.scrollBarV.ValueChanged += new System.EventHandler(this.scrollBarV_ValueChanged);
			// 
			// MainForm
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(693, 434);
			this.Controls.Add(this.scrollBarV);
			this.Controls.Add(this.mainBox);
			this.Name = "MainForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Uide";
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
			this.Resize += new System.EventHandler(this.MainForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.mainBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox mainBox;
		private System.Windows.Forms.VScrollBar scrollBarV;
	}
}