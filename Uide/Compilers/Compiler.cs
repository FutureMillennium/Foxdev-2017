using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilers
{
	abstract class Compiler
	{
		internal List<OutputMessage> outputMessages = new List<OutputMessage>();
		internal string outputFilePath;

		abstract internal bool Compile(string filePath);
	}
}
