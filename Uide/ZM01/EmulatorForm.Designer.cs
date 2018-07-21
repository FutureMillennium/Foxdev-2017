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
			this.teletypeBox = new Uide.SelectablePictureBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.startButton = new System.Windows.Forms.Button();
			this.stopButton = new System.Windows.Forms.Button();
			this.stepButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.teletypeBox)).BeginInit();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// teletypeBox
			// 
			this.teletypeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.teletypeBox.BackColor = System.Drawing.Color.White;
			this.teletypeBox.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
			this.teletypeBox.ForeColor = System.Drawing.Color.Black;
			this.teletypeBox.Location = new System.Drawing.Point(0, 0);
			this.teletypeBox.Name = "teletypeBox";
			this.teletypeBox.Size = new System.Drawing.Size(654, 364);
			this.teletypeBox.TabIndex = 0;
			this.teletypeBox.Paint += new System.Windows.Forms.PaintEventHandler(this.teletypeBox_Paint);
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this.startButton);
			this.flowLayoutPanel1.Controls.Add(this.stopButton);
			this.flowLayoutPanel1.Controls.Add(this.stepButton);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 367);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(654, 34);
			this.flowLayoutPanel1.TabIndex = 2;
			// 
			// startButton
			// 
			this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.startButton.Location = new System.Drawing.Point(0, 0);
			this.startButton.Margin = new System.Windows.Forms.Padding(0);
			this.startButton.Name = "startButton";
			this.startButton.Size = new System.Drawing.Size(112, 34);
			this.startButton.TabIndex = 3;
			this.startButton.Text = "Start";
			this.startButton.UseVisualStyleBackColor = true;
			this.startButton.Click += new System.EventHandler(this.startButton_Click);
			// 
			// stopButton
			// 
			this.stopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.stopButton.Enabled = false;
			this.stopButton.Location = new System.Drawing.Point(112, 0);
			this.stopButton.Margin = new System.Windows.Forms.Padding(0);
			this.stopButton.Name = "stopButton";
			this.stopButton.Size = new System.Drawing.Size(112, 34);
			this.stopButton.TabIndex = 4;
			this.stopButton.Text = "Stop";
			this.stopButton.UseVisualStyleBackColor = true;
			this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
			// 
			// stepButton
			// 
			this.stepButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.stepButton.Location = new System.Drawing.Point(224, 0);
			this.stepButton.Margin = new System.Windows.Forms.Padding(0);
			this.stepButton.Name = "stepButton";
			this.stepButton.Size = new System.Drawing.Size(112, 34);
			this.stepButton.TabIndex = 2;
			this.stepButton.Text = "Step";
			this.stepButton.UseVisualStyleBackColor = true;
			// 
			// EmulatorForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(654, 401);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Controls.Add(this.teletypeBox);
			this.Name = "EmulatorForm";
			this.Text = "ZM01 Emulator by Zdeněk Gromnica";
			((System.ComponentModel.ISupportInitialize)(this.teletypeBox)).EndInit();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private Uide.SelectablePictureBox teletypeBox;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button stepButton;
		private System.Windows.Forms.Button startButton;
		private System.Windows.Forms.Button stopButton;
	}
}