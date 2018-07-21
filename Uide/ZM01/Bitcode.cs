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
		sp,

		nop,
		hlt,
		cli,
		sti,
		jmp,
		iret,
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

	enum Instruction
	{
		nop = 0b00_000_000,
		hlt = 0b00_001_000,
		cli = 0b00_010_000,
		sti = 0b00_011_000,
		jmp = 0b00_100_000,
		iret = 0b00_101_000,
		jne = 0b00_110_000,
		je = 0b00_111_000,

		movRsR = 0b01_000_000,
		cmpRs = 0b01_000_000,
		addRs = 0b01_000_000,
		ldrRsB = 0b01_000_000,
		strRsB = 0b01_000_000,
		ldrRsL = 0b01_000_000,
		strRsL = 0b01_000_000,
		not = 0b01_000_000,
		xor = 0b01_000_000,
		mul = 0b01_000_000,
		div = 0b01_000_000,
		sub = 0b01_000_000,
		or = 0b01_000_000,
		and = 0b01_000_000,

		addImmB = 0b10_000_000,
		movRImmL = 0b11_000_000,


		movRR = 0b00_000_001,
		cmp = 0b00_000_010,
		add = 0b00_000_011,
		ldrB = 0b00_000_100,
		strB = 0b00_000_101,
		ldrL = 0b00_000_110,
		strL = 0b00_000_111,
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
			0b00_101_000,
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

	enum Registers
	{
		rz = 0,
		r1,
		r2,
		r3,
		r4,
		r5,
		r6,
		r7,
		sp,
		lr,
	}
}
