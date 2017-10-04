using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxlang
{
	enum LexingState { Normal, IgnoringUntilNewLine, ReadingString, ReadingDoubleString, NestedComments }
	enum ParsingState { HashCompile, HashCompileBlock, ComposeString, OutputProjectAssign, AddFileProject, HashCompileRunBlock, AddRunFileProject, Const, FunctionBlock, FunctionArguments, ValueParsing, ArrayAccess, While, Condition }
	enum FoxlangType { Byte, Uint8, Char, Int8, Byte2, Uint16, Int16, Byte4, Address4, Index, Uint, Uint32, Pointer, Int, Int32, String }
	enum Block { Namespace, Function }
	enum Bits { Bits16, Bits32 }

	public class Token
	{
		public string token;
		public int line,
			col,
			pos;
	}

	class Var
	{
		public string symbol;
		public FoxlangType type;
		public dynamic value;
	}

	class SymbolReference
	{
		public int pos;
		public long bytePos;
		public string symbol;
	}

	class VarReference
	{
		public int pos;
		public Var var;
	}

	class UnresolvedReference
	{
		public int pos;
		public long bytePos;
		public int bytes;
		public string symbol;
		public string filename;
		public Token token;
		public SymbolReference reference;
		public bool isAbsolute = false;
	}

	class Function
	{
		public string symbol;
		public Bits bits = Bits.Bits32;
		public List<Var> arguments = new List<Var>();
		public List<ByteCode> byteCode = new List<ByteCode>();
		public List<UnresolvedReference> unresolvedReferences = new List<UnresolvedReference>();
		public List<UnresolvedReference> urLabelsUnresolved = new List<UnresolvedReference>();
		public List<UnresolvedReference> urVarsUnresolved = new List<UnresolvedReference>();
		public List<VarReference> varReferences = new List<VarReference>();
		public List<SymbolReference> literalReferences = new List<SymbolReference>();
		public List<SymbolReference> labels = new List<SymbolReference>();
	}

	public class OutputMessage
	{
		public enum MessageType { Notice, Warning, Error }

		public MessageType type;
		public string message,
			filename;
		public Token token;
	}

	public class UnitInfo
	{
		public enum Format { Invalid, Flat }

		public uint relativeAddress = 0;
		public string name,
			entryPoint,
			extension,
			output;
		public Format format;
		public List<string> files,
			run;
	}

	internal class Project
	{
		internal string name;
	}
}
