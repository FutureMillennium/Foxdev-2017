using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxlangAlpha
{
	partial class FoxlangCompiler
	{
		// shared with CompileUnit:
		public List<OutputMessage> outputMessages = new List<OutputMessage>();
		public List<UnitInfo> units = new List<UnitInfo>();

		internal List<Function> functions = new List<Function>();
		internal List<Var> consts = new List<Var>();
		internal List<Var> vars = new List<Var>();
		internal UnitInfo curUnit;
		internal Project curProject = new Project();
		// /

		List<string> stringLiterals = new List<string>();
		Function entryPoint;
		public string output;

		public bool CompileProject(string filePath)
		{


			void GlobalErrorMessage(string message)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Error,
					message = message,
					filename = filePath,
				});
			}

			/*void GlobalWarningMessage(string message)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Warning,
					message = message,
					filename = filePath,
				});
			}*/


			curProject.name = Path.GetFileNameWithoutExtension(filePath);
			CompileUnit compileUnit = new CompileUnit(this);

			try
			{
				if (compileUnit.Compile(filePath) == false)
				{
					return false;
				}
			}
			catch (InvalidSyntaxException ex)
			{
				outputMessages.Add(new OutputMessage
				{
					type = OutputMessage.MessageType.Error,
					message = ex.Message,
					token = ex.token,
					filename = ex.filename,
				});
				return false;
			}

			/*if (projects.Count == 0)
			{
				GlobalErrorMessage("No projects inside project file. This is probably bad.");
				return false;
			}*/

			string projectPath = Path.GetDirectoryName(filePath);

			for (int i = 0; i < units.Count; i++)
			{
				foreach (string wildFile in units[i].files)
				{
					//string f = file.Replace('/', '\\');
					string f = wildFile;
					if (f[0] == '/')
						f = f.Substring(1);

					if (f.Contains('*'))
					{
						string path = Path.Combine(projectPath, Path.GetDirectoryName(f));
						string[] files = Directory.GetFiles(path, Path.GetFileName(wildFile));

						foreach (string file in files)
						{
							compileUnit = new CompileUnit(this);

							if (compileUnit.Compile(file) == false)
							{
								return false;
							}
						}
					}
					else
					{
						GlobalErrorMessage("Non-wildcard file names not implemented yet.");
					}
				}
			}

			foreach (Function f in functions)
			{
				foreach (UnresolvedReference r in f.unresolvedReferences)
				{
					bool resolved = false;
					foreach (Function ff in functions)
					{
						if (ff.symbol == r.symbol)
						{
							resolved = true;
							break;
						}
					}
					if (resolved == false)
					{
						outputMessages.Add(new OutputMessage
						{
							type = OutputMessage.MessageType.Error,
							message = "Unresolved symbol: " + r.symbol,
							token = r.token,
							filename = r.filename,
						});
						return false;
					}
				}
			}

			if (functions.Count == 0)
			{
				GlobalErrorMessage("No functions found. Nothing to compile.");
				return false;
			}

			if (units.Count == 0)
			{
				entryPoint = null;
				foreach (Function func in functions)
				{
					if (func.symbol == "EntryPoint")
					{
						entryPoint = func;
						break;
					}
				}
				if (entryPoint == null)
				{
					GlobalErrorMessage("No projects found and no function EntryPoint().");
					return false;
				}
				// @TODO
			}

			var curFunction = functions[0]; // @TODO more functions
			foreach (var r in curFunction.urLabelsUnresolved) // @TODO @cleanup
			{
				bool AddError(string message) // @TODO @cleanup
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = r.token,
						filename = filePath,
					});
					return false;
				}

				if (r.reference != null)
					continue;

				SymbolReference foundSym = null;
				foreach (var sym in curFunction.labels)
				{
					if (sym.symbol == r.symbol)
					{
						foundSym = sym;
						break;
					}
				}

				if (foundSym == null)
					return AddError("Label not found.");

				r.reference = foundSym;
				if (r.isAbsolute)
					curFunction.byteCode[r.pos] = (ByteCode)(foundSym.pos);
				else
					curFunction.byteCode[r.pos] = (ByteCode)(foundSym.pos - (r.pos + 1));
			}

			foreach (var r in curFunction.urVarsUnresolved) // @TODO @cleanup
			{
				bool AddError(string message) // @TODO @cleanup
				{
					outputMessages.Add(new OutputMessage
					{
						type = OutputMessage.MessageType.Error,
						message = message,
						token = r.token,
						filename = filePath,
					});
					return false;
				}

				Var foundVar;
				if (FindVar(r.symbol, out foundVar, consts) == false && FindVar(r.symbol, out foundVar, vars) == false) // @TODO actual vars
					return AddError("Undefined: " + r.symbol);

				curFunction.byteCode[r.pos] = (ByteCode)(foundVar.value);
			}



			curUnit = compileUnit.curUnit;

			string outputFile;
			
			if (curUnit.extension != null)
				outputFile = Path.ChangeExtension(filePath, curUnit.extension);
			else
				outputFile = Path.ChangeExtension(filePath, ".com"); // @TODO non-.com binaries?

			output = outputFile;

			if (BytecodeCompileToBinary(outputFile) == false)
				return false; // @TODO error

			return true;
		}

		static internal bool ParseLiteral(string strVal, out UInt32 ii)
		{
			UInt32 multiplier = 1;
			System.Globalization.NumberStyles baseNum = System.Globalization.NumberStyles.Integer;

			if (strVal.Length > 2)
				if (strVal.StartsWith("0x"))
				{
					strVal = strVal.Substring(2);
					baseNum = System.Globalization.NumberStyles.HexNumber;
				}
			/*else if (strVal.StartsWith("0b"))
			{
				strVal = strVal.Substring(2);
				baseNum = System.Globalization.NumberStyles.HexNumber;
			}*/ // @TODO binary and octal literals UGH C# WHY

			if (strVal.Last() == 'M')
			{
				strVal = strVal.Substring(0, strVal.Length - 1);
				multiplier = 1024 * 1024;
			}

			if (UInt32.TryParse(strVal, baseNum,
System.Globalization.CultureInfo.CurrentCulture, out ii))
			{
				ii *= multiplier;
				return true;
			}
			else
			{
				return false;
			}
		}

		static internal bool StringLiteralTryParse(string literal, out string outS)
		{
			if ((literal[0] == '"' && literal.Last() == '"') || (literal[0] == '\'' && literal.Last() == '\''))
			{
				outS = literal.Substring(1, literal.Length - 2);
				return true;
			}

			outS = null;
			return false;
		}

		static internal bool RegisterTryParse(string token, out ByteCode register, out int width)
		{
			if (token.Length >= 2)
			{
				token = (char)(token[1] - ('a' - 'A')) + token.Substring(2);
			}

			width = 0;

			if (Enum.TryParse(token, out register) == false)
				return false;

			switch (register)
			{
				case ByteCode.Al:
				case ByteCode.Cl:
				case ByteCode.Dl:
				case ByteCode.Bl:
				case ByteCode.Ah:
				case ByteCode.Ch:
				case ByteCode.Dh:
				case ByteCode.Bh:
					width = 1;
					break;
				case ByteCode.Ax:
				case ByteCode.Cx:
				case ByteCode.Dx:
				case ByteCode.Bx:
				case ByteCode.Sp:
				case ByteCode.Bp:
				case ByteCode.Si:
				case ByteCode.Di:
					width = 2;
					break;
				case ByteCode.Eax:
				case ByteCode.Ecx:
				case ByteCode.Edx:
				case ByteCode.Ebx:
				case ByteCode.Esp:
				case ByteCode.Ebp:
				case ByteCode.Esi:
				case ByteCode.Edi:
					width = 4;
					break;
				default:
					return false;
			}

			return true;
		}

		byte RegisterNumber(ByteCode register)
		{
			switch(register)
			{
				case ByteCode.Al:
				case ByteCode.Ax:
				case ByteCode.Eax:
					return 0;
				case ByteCode.Cl:
				case ByteCode.Cx:
				case ByteCode.Ecx:
					return 1;
				case ByteCode.Dl:
				case ByteCode.Dx:
				case ByteCode.Edx:
					return 2;
				case ByteCode.Bl:
				case ByteCode.Bx:
				case ByteCode.Ebx:
					return 3;
				case ByteCode.Sp:
				case ByteCode.Esp:
				case ByteCode.Ah:
					return 4;
				case ByteCode.Bp:
				case ByteCode.Ebp:
				case ByteCode.Ch:
					return 5;
				case ByteCode.Si:
				case ByteCode.Esi:
				case ByteCode.Dh:
					return 6;
				case ByteCode.Di:
				case ByteCode.Edi:
				case ByteCode.Bh:
					return 7;
			}
			return 0xFF;
		}

		byte RealModeRegisterNumber(ByteCode register)
		{
			/*
			67 8a 00                mov    al,BYTE PTR [bx+si]
			67 8a 01                mov    al,BYTE PTR [bx+di]
			67 8a 02                mov    al,BYTE PTR [bp+si]
			67 8a 03                mov    al,BYTE PTR [bp+di]
			67 8a 04                mov    al,BYTE PTR [si]
			67 8a 05                mov    al,BYTE PTR [di]
			67 8a 06 90 90          mov    al,BYTE PTR ds:0x9090
			67 8a 07                mov    al,BYTE PTR [bx]
			*/
			switch (register)
			{
				case ByteCode.Si:
					return 4;
				case ByteCode.Di:
					return 5;
				case ByteCode.Bx:
					return 7;
			}
			return 0xFF;
		}

		bool FindVar(string symbol, out Var foundVar, List<Var> lookInList)
		{
			foundVar = null;
			foreach (Var v in lookInList)
			{
				if (v.symbol == symbol)
				{
					foundVar = v;
					return true;
				}
			}

			return false;
		}

		static void Swap<T>(ref T left, ref T right)
		{
			T temp = left;
			left = right;
			right = temp;
		}
	}
}
