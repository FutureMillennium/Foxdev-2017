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

		nop = 0b00_000_000,

		hlt = 0b00_001_000,
		movRsR = 0b10_001_000,

		cmpRs = 0b10_010_000,

		addRs = 0b10_011_000,
		subRs = 0b10_100_000,
		orRs = 0b10_101_000,
		andRs = 0b10_110_000,
		not = 0b00_010_000,
		xor = 0b00_011_000,

		mul = 0b00_100_000,
		div = 0b00_101_000,

		jmpImm = 0b00_110_000,
		movRImmB = 0b00_111_000,
		movRImmL = 0b10_111_000,
		jneImm = 0b01_000_000,
		jeImm = 0b11_000_000,
		cli = 0b01_001_000,
		sti = 0b11_001_000,

		addImmB = 0b011_00_000,
		movRgImmL = 0b111_00_000,


		movRR = 0b00_000_001,

		cmp = 0b00_000_010,

		add = 0b00_000_011,
		sub = 0b00_000_100,
		or = 0b00_000_101,

		and = 0b00_000_110,

		ldrB = 0b00_000_111,
		ldrL = 0b01_000_111,
		strB = 0b10_000_111,
		strL = 0b11_000_111,
	}

	/*class BCProperties
	{

	}
	
	static class Properties
	{
		static Dictionary<Bitcode, BCProperties> ar = new Dictionary<Bitcode, BCProperties>
		{
			{ Bitcode.nop, new BCProperties {} },
		};
	}*/
}
