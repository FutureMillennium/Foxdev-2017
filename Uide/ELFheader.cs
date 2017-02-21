using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uide
{
	[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
	public struct ELFidentification // e_ident
	{
		//public const int EI_NIDENT = 16;

		public enum ClassBitNumber : byte { None, Bit32, Bit64 };
		public enum Endianness : byte { None, LittleEndian, BigEndian };

		// 0–3 e_ident[EI_MAG0] – e_ident[EI_MAG3]
		[System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 4)]
		public string e_ident;

		// e_ident[EI_CLASS]
		public ClassBitNumber bitNumber;

		// e_ident[EI_DATA]
		public Endianness endianness;

		// e_ident[EI_VERSION];
		public byte version;

		// e_ident[EI_OSABI]
		public byte targetOS;

		// e_ident[EI_ABIVERSION]
		public byte abiVersion;

		// e_ident[EI_PAD]
		[System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 7)]
		public string padding;
	}

	[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
	public struct ELFheader32 //Elf32_Ehdr
	{

		/// unsigned char[16] e_ident
		public ELFidentification identification;

		/// Elf32_Half->unsigned short e_type
		public ushort type;

		/// Elf32_Half->unsigned short e_machine
		public ushort machine;

		/// Elf32_Word->unsigned int e_version
		public uint version;

		/// Elf32_Addr->unsigned int e_entry
		public uint entryAddress;

		/// Elf32_Off->unsigned int e_phoff
		public uint programHeaderTableOffset;

		/// Elf32_Off->unsigned int e_shoff
		public uint sectionHeaderTableOffset;

		/// Elf32_Word->unsigned int e_flags
		public uint flags;

		/// Elf32_Half->unsigned short e_ehsize
		public ushort size;

		/// Elf32_Half->unsigned short e_phentsize
		public ushort sizeProgramHeaderTableEntry;

		/// Elf32_Half->unsigned short e_phnum
		public ushort numProgramHeaderTableEntry;

		/// Elf32_Half->unsigned short e_shentsize
		public ushort sizeSectionHeaderTableEntry;

		/// Elf32_Half->unsigned short e_shnum
		public ushort numSectionHeaderTableEntry;

		/// Elf32_Half->unsigned short e_shstrndx
		public ushort stringTableIndex;
	}

	[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
	public struct ELFheader64 // Elf64_Ehdr
	{

		/// unsigned char[16]
		[System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 16)]
		public string e_ident;

		/// Elf64_Half->unsigned short
		public ushort e_type;

		/// Elf64_Half->unsigned short
		public ushort e_machine;

		/// Elf64_Word->unsigned int
		public uint e_version;

		/// Elf64_Addr->unsigned int
		public uint e_entry;

		/// Elf64_Off->unsigned int
		public uint e_phoff;

		/// Elf64_Off->unsigned int
		public uint e_shoff;

		/// Elf64_Word->unsigned int
		public uint e_flags;

		/// Elf64_Half->unsigned short
		public ushort e_ehsize;

		/// Elf64_Half->unsigned short
		public ushort e_phentsize;

		/// Elf64_Half->unsigned short
		public ushort e_phnum;

		/// Elf64_Half->unsigned short
		public ushort e_shentsize;

		/// Elf64_Half->unsigned short
		public ushort e_shnum;

		/// Elf64_Half->unsigned short
		public ushort e_shstrndx;
	}

	[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
	public struct Elf32_Phdr
	{

		/// Elf32_Word->unsigned int
		public uint p_type;

		/// Elf32_Off->unsigned int
		public uint p_offset;

		/// Elf32_Addr->unsigned int
		public uint p_vaddr;

		/// Elf32_Addr->unsigned int
		public uint p_paddr;

		/// Elf32_Word->unsigned int
		public uint p_filesz;

		/// Elf32_Word->unsigned int
		public uint p_memsz;

		/// Elf32_Word->unsigned int
		public uint p_flags;

		/// Elf32_Word->unsigned int
		public uint p_align;
	}

	[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
	public struct Elf32_Shdr
	{
		/// Elf32_Word->unsigned int
		public uint sh_name;

		/// Elf32_Word->unsigned int
		public uint sh_type;

		/// Elf32_Word->unsigned int
		public uint sh_flags;

		/// Elf32_Addr->unsigned int
		public uint sh_addr;

		/// Elf32_Off->unsigned int
		public uint sh_offset;

		/// Elf32_Word->unsigned int
		public uint sh_size;

		/// Elf32_Word->unsigned int
		public uint sh_link;

		/// Elf32_Word->unsigned int
		public uint sh_info;

		/// Elf32_Word->unsigned int
		public uint sh_addralign;

		/// Elf32_Word->unsigned int
		public uint sh_entsize;
	}

}
