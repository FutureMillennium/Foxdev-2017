using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uide
{
	class ELFheader
	{
		public enum ClassBitNumber : byte { Bit32, Bit64 };
		public enum Endianness : byte { LittleEndian, BigEndian };

		public ClassBitNumber bitNumber; // e_ident[EI_CLASS]
		public Endianness endianness; // e_ident[EI_DATA]
		public byte version; // e_ident[EI_VERSION];
		public byte targetOS; // e_ident[EI_OSABI]
							  // e_ident[EI_ABIVERSION]
							  // e_ident[EI_PAD]
	}
}
