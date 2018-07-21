namespace ZM01
{
	partial class EmulatorForm
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
			this.teletypeTextBox = new System.Windows.Forms.TextBox();
			this.stepButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// teletypeTextBox
			// 
			this.teletypeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.teletypeTextBox.BackColor = System.Drawing.Color.Black;
			this.teletypeTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.teletypeTextBox.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.teletypeTextBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
			this.teletypeTextBox.Location = new System.Drawing.Point(0, 0);
			this.teletypeTextBox.Multiline = true;
			this.teletypeTextBox.Name = "teletypeTextBox";
			this.teletypeTextBox.ReadOnly = true;
			this.teletypeTextBox.Size = new System.Drawing.Size(654, 361);
			this.teletypeTextBox.TabIndex = 0;
			// 
			// stepButton
			// 
			this.stepButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.stepButton.Location = new System.Drawing.Point(0, 367);
			this.stepButton.Name = "stepButton";
			this.stepButton.Size = new System.Drawing.Size(112, 34);
			this.stepButton.TabIndex = 1;
			this.stepButton.Text = "Step";
			this.stepButton.UseVisualStyleBackColor = true;
			this.stepButton.Click += new System.EventHandler(this.stepButton_Click);
			// 
			// EmulatorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(654, 401);
			this.Controls.Add(this.stepButton);
			this.Controls.Add(this.teletypeTextBox);
			this.Name = "EmulatorForm";
			this.Text = "ZM01 Emulator by Zdeněk Gromnica";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox teletypeTextBox;
		private System.Windows.Forms.Button stepButton;
	}
}