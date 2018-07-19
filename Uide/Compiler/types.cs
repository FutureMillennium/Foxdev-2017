using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
	internal class OutputMessage
	{
		internal enum MessageType { Notice, Warning, Error }

		internal MessageType type;
		internal string message,
			filename;
		internal Token token;
	}
}
