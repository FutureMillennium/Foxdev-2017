using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilers
{
	internal class CompilerException : Exception
	{
		public string filename;
		public Token token;

		/*public CompilerException()
		{
		}

		public CompilerException(string message) : base(message)
		{
		}

		public CompilerException(string message, Exception inner) : base(message, inner)
		{
		}*/

		public CompilerException(string message, Token token, string filename) : base(message)
		{
			this.token = token;
			this.filename = filename;
		}
	}
}
