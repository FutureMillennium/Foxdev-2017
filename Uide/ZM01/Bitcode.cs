using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZM01
{
	internal enum Bitcode : byte {
		rz = 0,
		r1,
		r2,
		r3,
		r4,
		r5,
		r6,
		r7,

		nop,
		hlt,
		cli,
		sti,
		jmp,

		jne,
		je,

		movRsR,
		cmpRs,
		addRs,
		ldrRsB,
		strRsB,
		ldrRsL,
		strRsL,
		not,
		xor,
		mul,
		div,
		sub,
		or,
		and,

		addImmB,
		movRImmL,
		movRR,
		cmp,
		add,
		ldrB,
		strB,
		ldrL,
		strL,
	}

	/*class BCProperties
	{

	}*/
	
	static class BitcodeInfo
	{
		internal static byte[] lsb = new byte[] {
			0b00_000_000,
			0b00_001_000,
			0b00_010_000,
			0b00_011_000,
			0b00_100_000,

			0b00_110_000,
			0b00_111_000,

			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,
			0b01_000_000,

			0b10_000_000,
			0b11_000_000,
			0b00_000_001,
			0b00_000_010,
			0b00_000_011,
			0b00_000_100,
			0b00_000_101,
			0b00_000_110,
			0b00_000_111,
		};

		internal static byte[] msb = new byte[] {
			0b00001_000,
			0b00010_000,
			0b00011_000,
			0b00100_000,
			0b00101_000,
			0b00110_000,
			0b00111_000,
			0b01000_000,
			0b01001_000,
			0b01010_000,
			0b01011_000,
			0b01100_000,
			0b01101_000,
			0b01110_000,
		};

		/*static Dictionary<Bitcode, BCProperties> ar = new Dictionary<Bitcode, BCProperties>
		{
			{ Bitcode.nop, new BCProperties {} },
		};*/
	}
}
