using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxasm
{
	public enum Registers
	{
		// registers:
		// 8-bit:
		Al, Cl, Dl, Bl,
		Ah, Ch, Dh, Bh,
		// 16-bit:
		Ax, Cx, Dx, Bx, Sp, Bp, Si, Di,
		// 32-bit:
		Eax, Ecx, Edx, Ebx, Esp, Ebp, Esi, Edi,
	}

	enum Instructions
	{
		// Rm: mod-reg-R/M
		// R: register
		// S: segment register (FS, GS, CS, SS, DS, ES)
		// Imm: immediate value
		// B: byte, 8 bits
		// W: word – 2 bytes, 16 bits
		// L: long – 4 bytes, 32 bits
		// Q: quadword – 8 bytes, 64 bits
		Cli, Sti, Hlt,

		Call,
		Jmp, JmpB, JmpW, JmpL,
		Mov, MovB, MovW, MovL,
		Push, PushW, PushL,
		Pop, PopW, PopL,
		CmpB,
		Inc,
		Ret,
		Int,
		Je, Jne,
		LodsB,
	}

	enum Placeholders : UInt32
	{
		Label = 0xFEED1AFE,
		String = 0xFEED57FE,
		Variable = 0xFEED5AFE,
	}
}
