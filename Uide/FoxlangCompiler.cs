using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uide
{
	class FoxlangCompiler
	{
		enum ReadingState { Normal, IgnoringUntilNewLine, ReadingString }

		public void Compile(string filePath)
		{
			try
			{
				List<string> symbols = new List<string>();
				StreamReader streamReader = File.OpenText(filePath);

				char c, prevC = (char)0;
				string currentSymbol = "";
				ReadingState readingState = ReadingState.Normal;


				void AddSymbol()
				{
					if (currentSymbol.Length > 0)
					{
						symbols.Add(currentSymbol);
						currentSymbol = "";
					}
				}


				while (streamReader.EndOfStream == false)
				{
					c = (char)streamReader.Read();

					switch (readingState)
					{
						case ReadingState.IgnoringUntilNewLine:
							if (c == '\n')
							{
								currentSymbol = "";
								readingState = ReadingState.Normal;
							}
							break;
						case ReadingState.ReadingString:
							currentSymbol += c;
							if (c == '\'' && prevC != '\\')
							{
								readingState = ReadingState.Normal;
								AddSymbol();
							}
							break;
						case ReadingState.Normal:
							switch (c)
							{
								case '/':
									if (currentSymbol == "/")
									{
										readingState = ReadingState.IgnoringUntilNewLine;
									}
									else goto BreakingSymbol;
									break;
								case '\'':
									readingState = ReadingState.ReadingString;
									goto BreakingSymbol;
								case ';':
								case '(':
								case ')':
								BreakingSymbol:
									AddSymbol();
									goto default;
								case ' ':
								case '\t':
								case '\r':
								case '\n':
									AddSymbol();
									break;
								default:
									currentSymbol += c;
									break;
							}
							break;
					}

					prevC = c;
				}
			}
			catch (Exception e)
			{
				// @TODO
			}
		}
	}
}
