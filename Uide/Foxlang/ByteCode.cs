using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxlang
{
	public enum ByteCode : UInt32
	{
		Al, Bl, Cl, Dl,
		Ah, Bh, Ch, Dh,
		Ax, Bx, Cx, Dx, Sp, Bp, Si, Di,
		Eax, Ecx, Edx, Ebx, Esp, Ebp, Esi, Edi,

		// Rm: mod-reg-R/M
		// R: register
		// S: segment register (FS, GS, CS, SS, DS, ES)
		// Imm: immediate value
		// B: byte, 8 bits
		// W: word – 2 bytes, 16 bits
		// L: long – 4 bytes, 32 bits
		// Q: quadword – 8 bytes, 64 bits
		Cli, Sti, Hlt,

		Ret, Je, Jne, Int,

		// ByteCode only:
		CallRelW, CallRelL, CallRmW, CallRmL, CallPtrWW, CallPtrWL, CallMemWW, CallMemWL,
		JmpRelB, JmpRelW, JmpRelL, JmpRmW, JmpRmL, JmpPtrWW, JmpPtrWL, JmpMemWW, JmpMemWL,
		MovRmRB, //88
		MovRmRW, MovRmRL, //89
		MovRRmB, //8A
		MovRRmW, MovRRmL, //8B
		MovRmSW, MovSRmW, //8C //8E
						  // @TODO: A0, A1, A2, A3
		MovRImmB, //B0–B7 B0+ rb
		MovRImmW, MovRImmL, //B8–BF B8+ rw B8+ rd
		MovRmImmB, //C6 /0
		MovRmImmW, MovRmImmL, //C7 /0
							  // @TODO 0F 21/r, 0F 23 /r, 0F 22 /r, 0F 20 /r

		// mod (reg r/m):
		RRMem, // 00 xxx xxx
		RRMemOffset1, // 01 xxx xxx
		RRMemOffset4, // 10 xxx xxx
		RToR, // 11 xxx xxx

		RMemImm, // 00 xxx 101 – Displacement-Only Mode

		SIB, // 00 xxx 100 – SIB Mode
		SIBPlus1, // 01 xxx 100 – SIB + disp8 Mode
		SIBPlus4, // 10 xxx 100 – SIB + disp32 Mode

		// SIB (Scaled Index Byte):
		ZeroEiz, // xx 100 xxx
		DisplacementOnlyIndex, // 00 xxx xxx  xx xxx 101


		AddRMem,
		IncR,
		PopRW, PopRL,
		AddLMemImm,
		PushRmW, PushRML, //FF /6
		PushRW, PushRL, //50+rw	//50+rd
		PushImmB, //6A
		PushImmW, PushImmL, //68
		PushS, //0E //16 //1E //06 //0F A0 //0F A8
		PushA, PushAD, //60
		PushF, PushFD, //9C
		CmpRMemImmB, CmpRImmB,

		// Foxasm only:
		Call,
		Jmp, JmpB, JmpW, JmpL,
		Mov, MovB, MovW, MovL,
		Push, PushW, PushL,
		Pop, PopW, PopL,
		CmpB,
		Inc,

		// compiler/preprocessor only:
		Put4BytesHere, // dd
		// @TODO db, dw
		Align,

		// placeholders only:
		StringLiteralFeedMe = 0xFEED1133,
		LabelFeedMe = 0xFEED11E1,
		VarFeedMe = 0xFEED11E5,
		
	}
}
