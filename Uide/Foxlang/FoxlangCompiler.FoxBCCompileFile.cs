using System;
using System.Collections.Generic;
using System.IO;

namespace Foxlang
{
	partial class FoxlangCompiler
	{
		public bool FoxBCCompileFile(string filePath)
		{
			string fileExtension = null; // @TODO cleanup?

			List<Token> tokens = new List<Token>();

			LexerParse(filePath, tokens);

			Function curFunction = new Function();
			entryPoint = curFunction;

			int iMax = tokens.Count;
			int i = 0;

			while (i < iMax)
			{
				Token tok = tokens[i];
				string token = tok.token;
				ByteCode inByteCode;

				bool AddError(string message)
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = tok,
						filename = filePath,
					});
					return false;
				}

				if (Enum.TryParse(token, out inByteCode))
				{
					curFunction.byteCode.Add(inByteCode);
				}
				else
				{
					UInt32 ii;

					if (ParseLiteral(token, out ii))
					{
						curFunction.byteCode.Add((ByteCode)ii);
					}
					else
						return AddError("Invalid token.");
				}

				i++;
			}

			if (output == null)
			{
				if (fileExtension == null)
					fileExtension = ".bin"; // @TODO cleanup

				output = Path.ChangeExtension(filePath, fileExtension);
			}

			if (BytecodeCompileToBinary(output) == false)
				return false; // @TODO error

			return true;
		}
	}
}