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
		enum ParsingState { HashCompile, HashCompileBlock }

		public class OutputMessage
		{
			public enum MessageType { Warning, Error }

			public MessageType type;
			public string message,
				token;
			public int errorNumber;
		}

		public class Project
		{
			public enum Format { Invalid, Flat }

			public string name,
				entryPoint,
				output;
			public Format format;
			public List<string> files,
				run;
		}

		public List<string> tokens = new List<string>();
		public List<OutputMessage> outputMessages = new List<OutputMessage>();
		public List<Project> projects = new List<Project>();
		public string projectName;
		Project curProject;

		public void Compile(string filePath)
		{
			#region Lexical parsing
			try
			{
				projectName = Path.GetFileNameWithoutExtension(filePath);
				StreamReader streamReader = File.OpenText(filePath);

				char c, prevC = (char)0;
				string currentSymbol = "";
				ReadingState readingState = ReadingState.Normal;


				void AddSymbol()
				{
					if (currentSymbol.Length > 0)
					{
						tokens.Add(currentSymbol);
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

			// @TODO check invalid state (e.g. remaining in ParsingString)
			#endregion

			Stack<ParsingState> parsingStateStack = new Stack<ParsingState>();

			foreach (string token in tokens)
			{
				if (parsingStateStack.Count > 0)
				{
					ParsingState state = parsingStateStack.Peek();

					switch (state)
					{
						case ParsingState.HashCompile:
							if (token == "{")
							{
								parsingStateStack.Push(ParsingState.HashCompileBlock);
							}
							else
							{
								if (token[0] == '#')
								{
									// @TODO cleanup
									if (token == "#projectName")
										curProject.name = projectName;
									else
									{
										outputMessages.Add(new OutputMessage
										{
											type = OutputMessage.MessageType.Error,
											message = "Not implemented.", // @TODO line number
											token = token,
											errorNumber = 10001,
										});
										return;
									}
								}
								else if (token[0] == '\'')
								{
									curProject.name = token; // @TODO parse string
								}
								else
								{
									outputMessages.Add(new OutputMessage
									{
										type = OutputMessage.MessageType.Error,
										message = "Can't accept.", // @TODO line number
										token = token,
										errorNumber = 10000,
									});
									return;
								}
							}
							break;

						case ParsingState.HashCompileBlock:
							// @TODO
							break;
					}

				}
				else
				{
					switch (token)
					{
						case "#Compile":
							curProject = new Project();
							projects.Add(curProject);

							parsingStateStack.Push(ParsingState.HashCompile);
							break;
					}
				}
			}
		}
	}
}
