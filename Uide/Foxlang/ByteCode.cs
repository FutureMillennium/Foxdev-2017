using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxlang
{
	public enum ByteCode : UInt32
	{
		// registers:
		// 8-bit:
		Al, Cl, Dl, Bl,
		Ah, Ch, Dh, Bh,
		// 16-bit:
		Ax, Cx, Dx, Bx, Sp, Bp, Si, Di,
		// 32-bit:
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

		// ByteCode only:
		CallRelW, //E8 cw   CALL rel16  Call near, relative, displacement relative to next instruction
		CallRelL, //E8 cd   CALL rel32  Call near, relative, displacement relative to next instruction
		// @TODO
		//CallRmW, //FF /2	CALL r/m16  Call near, absolute indirect, address given in r/m16
		//CallRmL, //FF /2	CALL r/m32  Call near, absolute indirect, address given in r/m32
		//CallPtrWW, //9A cd   CALL ptr16:16	Call far, absolute, address given in operand
		//CallPtrWL, //9A cp   CALL ptr16:32	Call far, absolute, address given in operand
		//CallMemWW, //FF /3	CALL m16:16	Call far, absolute indirect, address given in m16:16
		//CallMemWL, //FF /3	CALL m16:32	Call far, absolute indirect, address given in m16:32

		RetNear, //C3	RET	Near return to calling procedure.
		// @TODO
		//CB	RET	Far return to calling procedure.
		//C2 iw	RET imm16	Near return to calling procedure and pop imm16 bytes from stack.
		//CA iw	RET imm16	Far return to calling procedure and pop imm16 bytes from stack.

		//CC	INT 3	Interrupt 3 - trap to debugger.
		IntImmB, //CD ib	INT imm8	Interrupt vector number specified by immediate byte.
		//CE	INTO	Interrupt 4 - if overflow flag is 1.

		JmpRelB, //EB cb	JMP rel8	Jump short, relative, displacement relative to next instruction.
		// @TODO
		//JmpRelW,
		//JmpRelL,
		//JmpRmW,
		//JmpRmL,
		//JmpPtrWW,
		//JmpPtrWL,
		//JmpMemWW,
		//JmpMemWL,
		//E9 cw	JMP rel16	Jump near, relative, displacement relative to next instruction.
		//E9 cd	JMP rel32	Jump near, relative, displacement relative to next instruction.
		//FF /4	JMP r/m16	Jump near, absolute indirect, address given in r/m16.
		//FF /4	JMP r/m32	Jump near, absolute indirect, address given in r/m32.
		//EA cd	JMP ptr16:16	Jump far, absolute, address given in operand.
		//EA cp	JMP ptr16:32	Jump far, absolute, address given in operand.
		//FF /5	JMP m16:16	Jump far, absolute indirect, address given in m16:16.
		//FF /5	JMP m16:32	Jump far, absolute indirect, address given in m16:32.

		JeRelB, // 74 cb	JE rel8	Jump short if equal (ZF=1).
		JneRelB, // 75 cb	JNE rel8	Jump short if not equal (ZF=0).

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
		//PushRmW, PushRmL, //FF /6
		PushRW, // 50+rw	PUSH r16	Push r16.
		PushRL, // 50+rd   PUSH r32    Push r32.
		PushImmB, //6A
		PushImmW, PushImmL, //68
		//PushS, //0E //16 //1E //06 //0F A0 //0F A8
		//PushA, PushAD, //60
		//PushF, PushFD, //9C
		CmpRMemImmB, CmpRImmB,

		// Foxasm only:
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
