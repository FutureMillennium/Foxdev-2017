using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxlang
{
	internal class InvalidSyntaxException : Exception
	{
		public string filename;
		public Token token;

		public InvalidSyntaxException()
		{
		}

		public InvalidSyntaxException(string message) : base(message)
		{
		}

		public InvalidSyntaxException(string message, Exception inner) : base(message, inner)
		{
		}

		public InvalidSyntaxException(string message, Token token, string filename) : base(message)
		{
			this.token = token;
			this.filename = filename;
		}
	}
}
