using System;
using System.Windows.Forms;

namespace Uide
{
	public class EntryPoint
	{
		[STAThread]
		public static void Main(string[] args)
		{
			/*var app = new App();
			app.InitializeComponent();
			app.Run();*/
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
