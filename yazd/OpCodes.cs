using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


// Ported and modified from http://z80ex.sourceforge.net/

namespace yazd
{
	[Flags]
	public enum OpCodeFlags
	{
		Continues = 0x0001,	// Instruction may continue at the next address
		Jumps = 0x0002,		// Instruction jumps to an absolute or relative address
		Returns = 0x0004,	// Instruction returns
		Restarts = 0x0008,	// Instruction jumps to restart address
		RefAddr = 0x0010,	// References a literal address
		PortRef = 0x0020,	// IN or OUT instruction
		Call = 0x0040,
	}

	public class OpCode
	{
		public string mnemonic;
		public int t_states;
		public int t_states2;
		public OpCodeFlags flags;

		public OpCode(string mnemonic, int t_states, int t_states2, OpCodeFlags flags = OpCodeFlags.Continues)
		{
			this.mnemonic = mnemonic;
			this.t_states = t_states;
			this.t_states2 = t_states2;
			this.flags = flags;

			if (mnemonic != null)
			{
				if (mnemonic.IndexOf("(@)") >= 0)
					this.flags |= OpCodeFlags.RefAddr;

				if (mnemonic.StartsWith("IN ") || mnemonic.StartsWith("OUT "))
					this.flags |= OpCodeFlags.PortRef;

				if (mnemonic.StartsWith("CALL ") || mnemonic.StartsWith("OUT "))
					this.flags |= OpCodeFlags.Call;
			}
		}
	}

	public static class OpCodes
	{
		/**/
		public static OpCode[] dasm_base = new OpCode[] 
		{
			new OpCode( "NOP"               ,  4 ,  0 ), /* 00 */
			new OpCode( "LD BC,@"           , 10 ,  0 ), /* 01 */
			new OpCode( "LD (BC),A"         ,  7 ,  0 ), /* 02 */
			new OpCode( "INC BC"            ,  6 ,  0 ), /* 03 */
			new OpCode( "INC B"             ,  4 ,  0 ), /* 04 */
			new OpCode( "DEC B"             ,  4 ,  0 ), /* 05 */
			new OpCode( "LD B,#"            ,  7 ,  0 ), /* 06 */
			new OpCode( "RLCA"              ,  4 ,  0 ), /* 07 */
			new OpCode( "EX AF,AF'"         ,  4 ,  0 ), /* 08 */
			new OpCode( "ADD HL,BC"         , 11 ,  0 ), /* 09 */
			new OpCode( "LD A,(BC)"         ,  7 ,  0 ), /* 0A */
			new OpCode( "DEC BC"            ,  6 ,  0 ), /* 0B */
			new OpCode( "INC C"             ,  4 ,  0 ), /* 0C */
			new OpCode( "DEC C"             ,  4 ,  0 ), /* 0D */
			new OpCode( "LD C,#"            ,  7 ,  0 ), /* 0E */
			new OpCode( "RRCA"              ,  4 ,  0 ), /* 0F */
			new OpCode( "DJNZ %"            ,  8 , 13 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* 10 */
			new OpCode( "LD DE,@"           , 10 ,  0 ), /* 11 */
			new OpCode( "LD (DE),A"         ,  7 ,  0 ), /* 12 */
			new OpCode( "INC DE"            ,  6 ,  0 ), /* 13 */
			new OpCode( "INC D"             ,  4 ,  0 ), /* 14 */
			new OpCode( "DEC D"             ,  4 ,  0 ), /* 15 */
			new OpCode( "LD D,#"            ,  7 ,  0 ), /* 16 */
			new OpCode( "RLA"               ,  4 ,  0 ), /* 17 */
			new OpCode( "JR %"              , 12 ,  0, OpCodeFlags.Jumps ), /* 18 */
			new OpCode( "ADD HL,DE"         , 11 ,  0 ), /* 19 */
			new OpCode( "LD A,(DE)"         ,  7 ,  0 ), /* 1A */
			new OpCode( "DEC DE"            ,  6 ,  0 ), /* 1B */
			new OpCode( "INC E"             ,  4 ,  0 ), /* 1C */
			new OpCode( "DEC E"             ,  4 ,  0 ), /* 1D */
			new OpCode( "LD E,#"            ,  7 ,  0 ), /* 1E */
			new OpCode( "RRA"               ,  4 ,  0 ), /* 1F */
			new OpCode( "JR NZ,%"           ,  7 , 12 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* 20 */
			new OpCode( "LD HL,@"           , 10 ,  0 ), /* 21 */
			new OpCode( "LD (@),HL"         , 16 ,  0 ), /* 22 */
			new OpCode( "INC HL"            ,  6 ,  0 ), /* 23 */
			new OpCode( "INC H"             ,  4 ,  0 ), /* 24 */
			new OpCode( "DEC H"             ,  4 ,  0 ), /* 25 */
			new OpCode( "LD H,#"            ,  7 ,  0 ), /* 26 */
			new OpCode( "DAA"               ,  4 ,  0 ), /* 27 */
			new OpCode( "JR Z,%"            ,  7 , 12 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* 28 */
			new OpCode( "ADD HL,HL"         , 11 ,  0 ), /* 29 */
			new OpCode( "LD HL,(@)"         , 16 ,  0 ), /* 2A */
			new OpCode( "DEC HL"            ,  6 ,  0 ), /* 2B */
			new OpCode( "INC L"             ,  4 ,  0 ), /* 2C */
			new OpCode( "DEC L"             ,  4 ,  0 ), /* 2D */
			new OpCode( "LD L,#"            ,  7 ,  0 ), /* 2E */
			new OpCode( "CPL"               ,  4 ,  0 ), /* 2F */
			new OpCode( "JR NC,%"           ,  7 , 12 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* 30 */
			new OpCode( "LD SP,@"           , 10 ,  0 ), /* 31 */
			new OpCode( "LD (@),A"          , 13 ,  0 ), /* 32 */
			new OpCode( "INC SP"            ,  6 ,  0 ), /* 33 */
			new OpCode( "INC (HL)"          , 11 ,  0 ), /* 34 */
			new OpCode( "DEC (HL)"          , 11 ,  0 ), /* 35 */
			new OpCode( "LD (HL),#"         , 10 ,  0 ), /* 36 */
			new OpCode( "SCF"               ,  4 ,  0 ), /* 37 */
			new OpCode( "JR C,%"            ,  7 , 12 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* 38 */
			new OpCode( "ADD HL,SP"         , 11 ,  0 ), /* 39 */
			new OpCode( "LD A,(@)"          , 13 ,  0 ), /* 3A */
			new OpCode( "DEC SP"            ,  6 ,  0 ), /* 3B */
			new OpCode( "INC A"             ,  4 ,  0 ), /* 3C */
			new OpCode( "DEC A"             ,  4 ,  0 ), /* 3D */
			new OpCode( "LD A,#"            ,  7 ,  0 ), /* 3E */
			new OpCode( "CCF"               ,  4 ,  0 ), /* 3F */
			new OpCode( "LD B,B"            ,  4 ,  0 ), /* 40 */
			new OpCode( "LD B,C"            ,  4 ,  0 ), /* 41 */
			new OpCode( "LD B,D"            ,  4 ,  0 ), /* 42 */
			new OpCode( "LD B,E"            ,  4 ,  0 ), /* 43 */
			new OpCode( "LD B,H"            ,  4 ,  0 ), /* 44 */
			new OpCode( "LD B,L"            ,  4 ,  0 ), /* 45 */
			new OpCode( "LD B,(HL)"         ,  7 ,  0 ), /* 46 */
			new OpCode( "LD B,A"            ,  4 ,  0 ), /* 47 */
			new OpCode( "LD C,B"            ,  4 ,  0 ), /* 48 */
			new OpCode( "LD C,C"            ,  4 ,  0 ), /* 49 */
			new OpCode( "LD C,D"            ,  4 ,  0 ), /* 4A */
			new OpCode( "LD C,E"            ,  4 ,  0 ), /* 4B */
			new OpCode( "LD C,H"            ,  4 ,  0 ), /* 4C */
			new OpCode( "LD C,L"            ,  4 ,  0 ), /* 4D */
			new OpCode( "LD C,(HL)"         ,  7 ,  0 ), /* 4E */
			new OpCode( "LD C,A"            ,  4 ,  0 ), /* 4F */
			new OpCode( "LD D,B"            ,  4 ,  0 ), /* 50 */
			new OpCode( "LD D,C"            ,  4 ,  0 ), /* 51 */
			new OpCode( "LD D,D"            ,  4 ,  0 ), /* 52 */
			new OpCode( "LD D,E"            ,  4 ,  0 ), /* 53 */
			new OpCode( "LD D,H"            ,  4 ,  0 ), /* 54 */
			new OpCode( "LD D,L"            ,  4 ,  0 ), /* 55 */
			new OpCode( "LD D,(HL)"         ,  7 ,  0 ), /* 56 */
			new OpCode( "LD D,A"            ,  4 ,  0 ), /* 57 */
			new OpCode( "LD E,B"            ,  4 ,  0 ), /* 58 */
			new OpCode( "LD E,C"            ,  4 ,  0 ), /* 59 */
			new OpCode( "LD E,D"            ,  4 ,  0 ), /* 5A */
			new OpCode( "LD E,E"            ,  4 ,  0 ), /* 5B */
			new OpCode( "LD E,H"            ,  4 ,  0 ), /* 5C */
			new OpCode( "LD E,L"            ,  4 ,  0 ), /* 5D */
			new OpCode( "LD E,(HL)"         ,  7 ,  0 ), /* 5E */
			new OpCode( "LD E,A"            ,  4 ,  0 ), /* 5F */
			new OpCode( "LD H,B"            ,  4 ,  0 ), /* 60 */
			new OpCode( "LD H,C"            ,  4 ,  0 ), /* 61 */
			new OpCode( "LD H,D"            ,  4 ,  0 ), /* 62 */
			new OpCode( "LD H,E"            ,  4 ,  0 ), /* 63 */
			new OpCode( "LD H,H"            ,  4 ,  0 ), /* 64 */
			new OpCode( "LD H,L"            ,  4 ,  0 ), /* 65 */
			new OpCode( "LD H,(HL)"         ,  7 ,  0 ), /* 66 */
			new OpCode( "LD H,A"            ,  4 ,  0 ), /* 67 */
			new OpCode( "LD L,B"            ,  4 ,  0 ), /* 68 */
			new OpCode( "LD L,C"            ,  4 ,  0 ), /* 69 */
			new OpCode( "LD L,D"            ,  4 ,  0 ), /* 6A */
			new OpCode( "LD L,E"            ,  4 ,  0 ), /* 6B */
			new OpCode( "LD L,H"            ,  4 ,  0 ), /* 6C */
			new OpCode( "LD L,L"            ,  4 ,  0 ), /* 6D */
			new OpCode( "LD L,(HL)"         ,  7 ,  0 ), /* 6E */
			new OpCode( "LD L,A"            ,  4 ,  0 ), /* 6F */
			new OpCode( "LD (HL),B"         ,  7 ,  0 ), /* 70 */
			new OpCode( "LD (HL),C"         ,  7 ,  0 ), /* 71 */
			new OpCode( "LD (HL),D"         ,  7 ,  0 ), /* 72 */
			new OpCode( "LD (HL),E"         ,  7 ,  0 ), /* 73 */
			new OpCode( "LD (HL),H"         ,  7 ,  0 ), /* 74 */
			new OpCode( "LD (HL),L"         ,  7 ,  0 ), /* 75 */
			new OpCode( "HALT"              ,  4 ,  0 ), /* 76 */
			new OpCode( "LD (HL),A"         ,  7 ,  0 ), /* 77 */
			new OpCode( "LD A,B"            ,  4 ,  0 ), /* 78 */
			new OpCode( "LD A,C"            ,  4 ,  0 ), /* 79 */
			new OpCode( "LD A,D"            ,  4 ,  0 ), /* 7A */
			new OpCode( "LD A,E"            ,  4 ,  0 ), /* 7B */
			new OpCode( "LD A,H"            ,  4 ,  0 ), /* 7C */
			new OpCode( "LD A,L"            ,  4 ,  0 ), /* 7D */
			new OpCode( "LD A,(HL)"         ,  7 ,  0 ), /* 7E */
			new OpCode( "LD A,A"            ,  4 ,  0 ), /* 7F */
			new OpCode( "ADD A,B"           ,  4 ,  0 ), /* 80 */
			new OpCode( "ADD A,C"           ,  4 ,  0 ), /* 81 */
			new OpCode( "ADD A,D"           ,  4 ,  0 ), /* 82 */
			new OpCode( "ADD A,E"           ,  4 ,  0 ), /* 83 */
			new OpCode( "ADD A,H"           ,  4 ,  0 ), /* 84 */
			new OpCode( "ADD A,L"           ,  4 ,  0 ), /* 85 */
			new OpCode( "ADD A,(HL)"        ,  7 ,  0 ), /* 86 */
			new OpCode( "ADD A,A"           ,  4 ,  0 ), /* 87 */
			new OpCode( "ADC A,B"           ,  4 ,  0 ), /* 88 */
			new OpCode( "ADC A,C"           ,  4 ,  0 ), /* 89 */
			new OpCode( "ADC A,D"           ,  4 ,  0 ), /* 8A */
			new OpCode( "ADC A,E"           ,  4 ,  0 ), /* 8B */
			new OpCode( "ADC A,H"           ,  4 ,  0 ), /* 8C */
			new OpCode( "ADC A,L"           ,  4 ,  0 ), /* 8D */
			new OpCode( "ADC A,(HL)"        ,  7 ,  0 ), /* 8E */
			new OpCode( "ADC A,A"           ,  4 ,  0 ), /* 8F */
			new OpCode( "SUB B"             ,  4 ,  0 ), /* 90 */
			new OpCode( "SUB C"             ,  4 ,  0 ), /* 91 */
			new OpCode( "SUB D"             ,  4 ,  0 ), /* 92 */
			new OpCode( "SUB E"             ,  4 ,  0 ), /* 93 */
			new OpCode( "SUB H"             ,  4 ,  0 ), /* 94 */
			new OpCode( "SUB L"             ,  4 ,  0 ), /* 95 */
			new OpCode( "SUB (HL)"          ,  7 ,  0 ), /* 96 */
			new OpCode( "SUB A"             ,  4 ,  0 ), /* 97 */
			new OpCode( "SBC A,B"           ,  4 ,  0 ), /* 98 */
			new OpCode( "SBC A,C"           ,  4 ,  0 ), /* 99 */
			new OpCode( "SBC A,D"           ,  4 ,  0 ), /* 9A */
			new OpCode( "SBC A,E"           ,  4 ,  0 ), /* 9B */
			new OpCode( "SBC A,H"           ,  4 ,  0 ), /* 9C */
			new OpCode( "SBC A,L"           ,  4 ,  0 ), /* 9D */
			new OpCode( "SBC A,(HL)"        ,  7 ,  0 ), /* 9E */
			new OpCode( "SBC A,A"           ,  4 ,  0 ), /* 9F */
			new OpCode( "AND B"             ,  4 ,  0 ), /* A0 */
			new OpCode( "AND C"             ,  4 ,  0 ), /* A1 */
			new OpCode( "AND D"             ,  4 ,  0 ), /* A2 */
			new OpCode( "AND E"             ,  4 ,  0 ), /* A3 */
			new OpCode( "AND H"             ,  4 ,  0 ), /* A4 */
			new OpCode( "AND L"             ,  4 ,  0 ), /* A5 */
			new OpCode( "AND (HL)"          ,  7 ,  0 ), /* A6 */
			new OpCode( "AND A"             ,  4 ,  0 ), /* A7 */
			new OpCode( "XOR B"             ,  4 ,  0 ), /* A8 */
			new OpCode( "XOR C"             ,  4 ,  0 ), /* A9 */
			new OpCode( "XOR D"             ,  4 ,  0 ), /* AA */
			new OpCode( "XOR E"             ,  4 ,  0 ), /* AB */
			new OpCode( "XOR H"             ,  4 ,  0 ), /* AC */
			new OpCode( "XOR L"             ,  4 ,  0 ), /* AD */
			new OpCode( "XOR (HL)"          ,  7 ,  0 ), /* AE */
			new OpCode( "XOR A"             ,  4 ,  0 ), /* AF */
			new OpCode( "OR B"              ,  4 ,  0 ), /* B0 */
			new OpCode( "OR C"              ,  4 ,  0 ), /* B1 */
			new OpCode( "OR D"              ,  4 ,  0 ), /* B2 */
			new OpCode( "OR E"              ,  4 ,  0 ), /* B3 */
			new OpCode( "OR H"              ,  4 ,  0 ), /* B4 */
			new OpCode( "OR L"              ,  4 ,  0 ), /* B5 */
			new OpCode( "OR (HL)"           ,  7 ,  0 ), /* B6 */
			new OpCode( "OR A"              ,  4 ,  0 ), /* B7 */
			new OpCode( "CP B"              ,  4 ,  0 ), /* B8 */
			new OpCode( "CP C"              ,  4 ,  0 ), /* B9 */
			new OpCode( "CP D"              ,  4 ,  0 ), /* BA */
			new OpCode( "CP E"              ,  4 ,  0 ), /* BB */
			new OpCode( "CP H"              ,  4 ,  0 ), /* BC */
			new OpCode( "CP L"              ,  4 ,  0 ), /* BD */
			new OpCode( "CP (HL)"           ,  7 ,  0 ), /* BE */
			new OpCode( "CP A"              ,  4 ,  0 ), /* BF */
			new OpCode( "RET NZ"            ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* C0 */
			new OpCode( "POP BC"            , 10 ,  0 ), /* C1 */
			new OpCode( "JP NZ,@"           , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* C2 */
			new OpCode( "JP @"              , 10 ,  0 , OpCodeFlags.Jumps), /* C3 */
			new OpCode( "CALL NZ,@"         , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* C4 */
			new OpCode( "PUSH BC"           , 11 ,  0 ), /* C5 */
			new OpCode( "ADD A,#"           ,  7 ,  0 ), /* C6 */
			new OpCode( "RST 0x00"          , 11 ,  0 , OpCodeFlags.Restarts), /* C7 */
			new OpCode( "RET Z"             ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* C8 */
			new OpCode( "RET"               , 10 ,  0 , OpCodeFlags.Returns ), /* C9 */
			new OpCode( "JP Z,@"            , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* CA */
			new OpCode( "shift CB"          ,  4 ,  0 ), /* CB */
			new OpCode( "CALL Z,@"          , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* CC */
			new OpCode( "CALL @"            , 17 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* CD */
			new OpCode( "ADC A,#"           ,  7 ,  0 ), /* CE */
			new OpCode( "RST 0x08"          , 11 ,  0 , OpCodeFlags.Restarts), /* CF */
			new OpCode( "RET NC"            ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* D0 */
			new OpCode( "POP DE"            , 10 ,  0 ), /* D1 */
			new OpCode( "JP NC,@"           , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* D2 */
			new OpCode( "OUT (#),A"         , 11 ,  0 ), /* D3 */
			new OpCode( "CALL NC,@"         , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* D4 */
			new OpCode( "PUSH DE"           , 11 ,  0 ), /* D5 */
			new OpCode( "SUB #"             ,  7 ,  0 ), /* D6 */
			new OpCode( "RST 0x10"          , 11 ,  0 , OpCodeFlags.Restarts), /* D7 */
			new OpCode( "RET C"             ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* D8 */
			new OpCode( "EXX"               ,  4 ,  0 ), /* D9 */
			new OpCode( "JP C,@"            , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* DA */
			new OpCode( "IN A,(#)"          , 11 ,  0 ), /* DB */
			new OpCode( "CALL C,@"          , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* DC */
			new OpCode( "shift DD"          ,  0 ,  0 ), /* DD */
			new OpCode( "SBC A,#"           ,  7 ,  0 ), /* DE */
			new OpCode( "RST 0x18"          , 11 ,  0 , OpCodeFlags.Restarts), /* DF */
			new OpCode( "RET PO"            ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* E0 */
			new OpCode( "POP HL"            , 10 ,  0 ), /* E1 */
			new OpCode( "JP PO,@"           , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* E2 */
			new OpCode( "EX (SP),HL"        , 19 ,  0 ), /* E3 */
			new OpCode( "CALL PO,@"         , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* E4 */
			new OpCode( "PUSH HL"           , 11 ,  0 ), /* E5 */
			new OpCode( "AND #"             ,  7 ,  0 ), /* E6 */
			new OpCode( "RST 0x20"          , 11 ,  0 , OpCodeFlags.Restarts), /* E7 */
			new OpCode( "RET PE"            ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* E8 */
			new OpCode( "JP (HL)"             ,  4 ,  0 , OpCodeFlags.Jumps), /* E9 */
			new OpCode( "JP PE,@"           , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* EA */
			new OpCode( "EX DE,HL"          ,  4 ,  0 ), /* EB */
			new OpCode( "CALL PE,@"         , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* EC */
			new OpCode( "shift ED"          ,  0 ,  0 ), /* ED */
			new OpCode( "XOR #"             ,  7 ,  0 ), /* EE */
			new OpCode( "RST 0x28"          , 11 ,  0 , OpCodeFlags.Restarts), /* EF */
			new OpCode( "RET P"             ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* F0 */
			new OpCode( "POP AF"            , 10 ,  0 ), /* F1 */
			new OpCode( "JP P,@"            , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* F2 */
			new OpCode( "DI"                ,  4 ,  0 ), /* F3 */
			new OpCode( "CALL P,@"          , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* F4 */
			new OpCode( "PUSH AF"           , 11 ,  0 ), /* F5 */
			new OpCode( "OR #"              ,  7 ,  0 ), /* F6 */
			new OpCode( "RST 0x30"          , 11 ,  0 , OpCodeFlags.Restarts), /* F7 */
			new OpCode( "RET M"             ,  5 , 11 , OpCodeFlags.Returns | OpCodeFlags.Continues), /* F8 */
			new OpCode( "LD SP,HL"          ,  6 ,  0 ), /* F9 */
			new OpCode( "JP M,@"            , 10 ,  0 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* FA */
			new OpCode( "EI"                ,  4 ,  0 ), /* FB */
			new OpCode( "CALL M,@"          , 10 , 17 , OpCodeFlags.Jumps | OpCodeFlags.Continues), /* FC */
			new OpCode( "shift FD"          ,  4 ,  0 ), /* FD */
			new OpCode( "CP #"              ,  7 ,  0 ), /* FE */
			new OpCode( "RST 0x38"          , 11 ,  0 , OpCodeFlags.Restarts), /* FF */
		};


		/**/
		public static OpCode[] dasm_cb = new OpCode[]
		{
			new OpCode( "RLC B"             ,  8 ,  0 ), /* 00 */
			new OpCode( "RLC C"             ,  8 ,  0 ), /* 01 */
			new OpCode( "RLC D"             ,  8 ,  0 ), /* 02 */
			new OpCode( "RLC E"             ,  8 ,  0 ), /* 03 */
			new OpCode( "RLC H"             ,  8 ,  0 ), /* 04 */
			new OpCode( "RLC L"             ,  8 ,  0 ), /* 05 */
			new OpCode( "RLC (HL)"          , 15 ,  0 ), /* 06 */
			new OpCode( "RLC A"             ,  8 ,  0 ), /* 07 */
			new OpCode( "RRC B"             ,  8 ,  0 ), /* 08 */
			new OpCode( "RRC C"             ,  8 ,  0 ), /* 09 */
			new OpCode( "RRC D"             ,  8 ,  0 ), /* 0A */
			new OpCode( "RRC E"             ,  8 ,  0 ), /* 0B */
			new OpCode( "RRC H"             ,  8 ,  0 ), /* 0C */
			new OpCode( "RRC L"             ,  8 ,  0 ), /* 0D */
			new OpCode( "RRC (HL)"          , 15 ,  0 ), /* 0E */
			new OpCode( "RRC A"             ,  8 ,  0 ), /* 0F */
			new OpCode( "RL B"              ,  8 ,  0 ), /* 10 */
			new OpCode( "RL C"              ,  8 ,  0 ), /* 11 */
			new OpCode( "RL D"              ,  8 ,  0 ), /* 12 */
			new OpCode( "RL E"              ,  8 ,  0 ), /* 13 */
			new OpCode( "RL H"              ,  8 ,  0 ), /* 14 */
			new OpCode( "RL L"              ,  8 ,  0 ), /* 15 */
			new OpCode( "RL (HL)"           , 15 ,  0 ), /* 16 */
			new OpCode( "RL A"              ,  8 ,  0 ), /* 17 */
			new OpCode( "RR B"              ,  8 ,  0 ), /* 18 */
			new OpCode( "RR C"              ,  8 ,  0 ), /* 19 */
			new OpCode( "RR D"              ,  8 ,  0 ), /* 1A */
			new OpCode( "RR E"              ,  8 ,  0 ), /* 1B */
			new OpCode( "RR H"              ,  8 ,  0 ), /* 1C */
			new OpCode( "RR L"              ,  8 ,  0 ), /* 1D */
			new OpCode( "RR (HL)"           , 15 ,  0 ), /* 1E */
			new OpCode( "RR A"              ,  8 ,  0 ), /* 1F */
			new OpCode( "SLA B"             ,  8 ,  0 ), /* 20 */
			new OpCode( "SLA C"             ,  8 ,  0 ), /* 21 */
			new OpCode( "SLA D"             ,  8 ,  0 ), /* 22 */
			new OpCode( "SLA E"             ,  8 ,  0 ), /* 23 */
			new OpCode( "SLA H"             ,  8 ,  0 ), /* 24 */
			new OpCode( "SLA L"             ,  8 ,  0 ), /* 25 */
			new OpCode( "SLA (HL)"          , 15 ,  0 ), /* 26 */
			new OpCode( "SLA A"             ,  8 ,  0 ), /* 27 */
			new OpCode( "SRA B"             ,  8 ,  0 ), /* 28 */
			new OpCode( "SRA C"             ,  8 ,  0 ), /* 29 */
			new OpCode( "SRA D"             ,  8 ,  0 ), /* 2A */
			new OpCode( "SRA E"             ,  8 ,  0 ), /* 2B */
			new OpCode( "SRA H"             ,  8 ,  0 ), /* 2C */
			new OpCode( "SRA L"             ,  8 ,  0 ), /* 2D */
			new OpCode( "SRA (HL)"          , 15 ,  0 ), /* 2E */
			new OpCode( "SRA A"             ,  8 ,  0 ), /* 2F */
			new OpCode( "SLL B"             ,  8 ,  0 ), /* 30 */
			new OpCode( "SLL C"             ,  8 ,  0 ), /* 31 */
			new OpCode( "SLL D"             ,  8 ,  0 ), /* 32 */
			new OpCode( "SLL E"             ,  8 ,  0 ), /* 33 */
			new OpCode( "SLL H"             ,  8 ,  0 ), /* 34 */
			new OpCode( "SLL L"             ,  8 ,  0 ), /* 35 */
			new OpCode( "SLL (HL)"          , 15 ,  0 ), /* 36 */
			new OpCode( "SLL A"             ,  8 ,  0 ), /* 37 */
			new OpCode( "SRL B"             ,  8 ,  0 ), /* 38 */
			new OpCode( "SRL C"             ,  8 ,  0 ), /* 39 */
			new OpCode( "SRL D"             ,  8 ,  0 ), /* 3A */
			new OpCode( "SRL E"             ,  8 ,  0 ), /* 3B */
			new OpCode( "SRL H"             ,  8 ,  0 ), /* 3C */
			new OpCode( "SRL L"             ,  8 ,  0 ), /* 3D */
			new OpCode( "SRL (HL)"          , 15 ,  0 ), /* 3E */
			new OpCode( "SRL A"             ,  8 ,  0 ), /* 3F */
			new OpCode( "BIT 0,B"           ,  8 ,  0 ), /* 40 */
			new OpCode( "BIT 0,C"           ,  8 ,  0 ), /* 41 */
			new OpCode( "BIT 0,D"           ,  8 ,  0 ), /* 42 */
			new OpCode( "BIT 0,E"           ,  8 ,  0 ), /* 43 */
			new OpCode( "BIT 0,H"           ,  8 ,  0 ), /* 44 */
			new OpCode( "BIT 0,L"           ,  8 ,  0 ), /* 45 */
			new OpCode( "BIT 0,(HL)"        , 12 ,  0 ), /* 46 */
			new OpCode( "BIT 0,A"           ,  8 ,  0 ), /* 47 */
			new OpCode( "BIT 1,B"           ,  8 ,  0 ), /* 48 */
			new OpCode( "BIT 1,C"           ,  8 ,  0 ), /* 49 */
			new OpCode( "BIT 1,D"           ,  8 ,  0 ), /* 4A */
			new OpCode( "BIT 1,E"           ,  8 ,  0 ), /* 4B */
			new OpCode( "BIT 1,H"           ,  8 ,  0 ), /* 4C */
			new OpCode( "BIT 1,L"           ,  8 ,  0 ), /* 4D */
			new OpCode( "BIT 1,(HL)"        , 12 ,  0 ), /* 4E */
			new OpCode( "BIT 1,A"           ,  8 ,  0 ), /* 4F */
			new OpCode( "BIT 2,B"           ,  8 ,  0 ), /* 50 */
			new OpCode( "BIT 2,C"           ,  8 ,  0 ), /* 51 */
			new OpCode( "BIT 2,D"           ,  8 ,  0 ), /* 52 */
			new OpCode( "BIT 2,E"           ,  8 ,  0 ), /* 53 */
			new OpCode( "BIT 2,H"           ,  8 ,  0 ), /* 54 */
			new OpCode( "BIT 2,L"           ,  8 ,  0 ), /* 55 */
			new OpCode( "BIT 2,(HL)"        , 12 ,  0 ), /* 56 */
			new OpCode( "BIT 2,A"           ,  8 ,  0 ), /* 57 */
			new OpCode( "BIT 3,B"           ,  8 ,  0 ), /* 58 */
			new OpCode( "BIT 3,C"           ,  8 ,  0 ), /* 59 */
			new OpCode( "BIT 3,D"           ,  8 ,  0 ), /* 5A */
			new OpCode( "BIT 3,E"           ,  8 ,  0 ), /* 5B */
			new OpCode( "BIT 3,H"           ,  8 ,  0 ), /* 5C */
			new OpCode( "BIT 3,L"           ,  8 ,  0 ), /* 5D */
			new OpCode( "BIT 3,(HL)"        , 12 ,  0 ), /* 5E */
			new OpCode( "BIT 3,A"           ,  8 ,  0 ), /* 5F */
			new OpCode( "BIT 4,B"           ,  8 ,  0 ), /* 60 */
			new OpCode( "BIT 4,C"           ,  8 ,  0 ), /* 61 */
			new OpCode( "BIT 4,D"           ,  8 ,  0 ), /* 62 */
			new OpCode( "BIT 4,E"           ,  8 ,  0 ), /* 63 */
			new OpCode( "BIT 4,H"           ,  8 ,  0 ), /* 64 */
			new OpCode( "BIT 4,L"           ,  8 ,  0 ), /* 65 */
			new OpCode( "BIT 4,(HL)"        , 12 ,  0 ), /* 66 */
			new OpCode( "BIT 4,A"           ,  8 ,  0 ), /* 67 */
			new OpCode( "BIT 5,B"           ,  8 ,  0 ), /* 68 */
			new OpCode( "BIT 5,C"           ,  8 ,  0 ), /* 69 */
			new OpCode( "BIT 5,D"           ,  8 ,  0 ), /* 6A */
			new OpCode( "BIT 5,E"           ,  8 ,  0 ), /* 6B */
			new OpCode( "BIT 5,H"           ,  8 ,  0 ), /* 6C */
			new OpCode( "BIT 5,L"           ,  8 ,  0 ), /* 6D */
			new OpCode( "BIT 5,(HL)"        , 12 ,  0 ), /* 6E */
			new OpCode( "BIT 5,A"           ,  8 ,  0 ), /* 6F */
			new OpCode( "BIT 6,B"           ,  8 ,  0 ), /* 70 */
			new OpCode( "BIT 6,C"           ,  8 ,  0 ), /* 71 */
			new OpCode( "BIT 6,D"           ,  8 ,  0 ), /* 72 */
			new OpCode( "BIT 6,E"           ,  8 ,  0 ), /* 73 */
			new OpCode( "BIT 6,H"           ,  8 ,  0 ), /* 74 */
			new OpCode( "BIT 6,L"           ,  8 ,  0 ), /* 75 */
			new OpCode( "BIT 6,(HL)"        , 12 ,  0 ), /* 76 */
			new OpCode( "BIT 6,A"           ,  8 ,  0 ), /* 77 */
			new OpCode( "BIT 7,B"           ,  8 ,  0 ), /* 78 */
			new OpCode( "BIT 7,C"           ,  8 ,  0 ), /* 79 */
			new OpCode( "BIT 7,D"           ,  8 ,  0 ), /* 7A */
			new OpCode( "BIT 7,E"           ,  8 ,  0 ), /* 7B */
			new OpCode( "BIT 7,H"           ,  8 ,  0 ), /* 7C */
			new OpCode( "BIT 7,L"           ,  8 ,  0 ), /* 7D */
			new OpCode( "BIT 7,(HL)"        , 12 ,  0 ), /* 7E */
			new OpCode( "BIT 7,A"           ,  8 ,  0 ), /* 7F */
			new OpCode( "RES 0,B"           ,  8 ,  0 ), /* 80 */
			new OpCode( "RES 0,C"           ,  8 ,  0 ), /* 81 */
			new OpCode( "RES 0,D"           ,  8 ,  0 ), /* 82 */
			new OpCode( "RES 0,E"           ,  8 ,  0 ), /* 83 */
			new OpCode( "RES 0,H"           ,  8 ,  0 ), /* 84 */
			new OpCode( "RES 0,L"           ,  8 ,  0 ), /* 85 */
			new OpCode( "RES 0,(HL)"        , 15 ,  0 ), /* 86 */
			new OpCode( "RES 0,A"           ,  8 ,  0 ), /* 87 */
			new OpCode( "RES 1,B"           ,  8 ,  0 ), /* 88 */
			new OpCode( "RES 1,C"           ,  8 ,  0 ), /* 89 */
			new OpCode( "RES 1,D"           ,  8 ,  0 ), /* 8A */
			new OpCode( "RES 1,E"           ,  8 ,  0 ), /* 8B */
			new OpCode( "RES 1,H"           ,  8 ,  0 ), /* 8C */
			new OpCode( "RES 1,L"           ,  8 ,  0 ), /* 8D */
			new OpCode( "RES 1,(HL)"        , 15 ,  0 ), /* 8E */
			new OpCode( "RES 1,A"           ,  8 ,  0 ), /* 8F */
			new OpCode( "RES 2,B"           ,  8 ,  0 ), /* 90 */
			new OpCode( "RES 2,C"           ,  8 ,  0 ), /* 91 */
			new OpCode( "RES 2,D"           ,  8 ,  0 ), /* 92 */
			new OpCode( "RES 2,E"           ,  8 ,  0 ), /* 93 */
			new OpCode( "RES 2,H"           ,  8 ,  0 ), /* 94 */
			new OpCode( "RES 2,L"           ,  8 ,  0 ), /* 95 */
			new OpCode( "RES 2,(HL)"        , 15 ,  0 ), /* 96 */
			new OpCode( "RES 2,A"           ,  8 ,  0 ), /* 97 */
			new OpCode( "RES 3,B"           ,  8 ,  0 ), /* 98 */
			new OpCode( "RES 3,C"           ,  8 ,  0 ), /* 99 */
			new OpCode( "RES 3,D"           ,  8 ,  0 ), /* 9A */
			new OpCode( "RES 3,E"           ,  8 ,  0 ), /* 9B */
			new OpCode( "RES 3,H"           ,  8 ,  0 ), /* 9C */
			new OpCode( "RES 3,L"           ,  8 ,  0 ), /* 9D */
			new OpCode( "RES 3,(HL)"        , 15 ,  0 ), /* 9E */
			new OpCode( "RES 3,A"           ,  8 ,  0 ), /* 9F */
			new OpCode( "RES 4,B"           ,  8 ,  0 ), /* A0 */
			new OpCode( "RES 4,C"           ,  8 ,  0 ), /* A1 */
			new OpCode( "RES 4,D"           ,  8 ,  0 ), /* A2 */
			new OpCode( "RES 4,E"           ,  8 ,  0 ), /* A3 */
			new OpCode( "RES 4,H"           ,  8 ,  0 ), /* A4 */
			new OpCode( "RES 4,L"           ,  8 ,  0 ), /* A5 */
			new OpCode( "RES 4,(HL)"        , 15 ,  0 ), /* A6 */
			new OpCode( "RES 4,A"           ,  8 ,  0 ), /* A7 */
			new OpCode( "RES 5,B"           ,  8 ,  0 ), /* A8 */
			new OpCode( "RES 5,C"           ,  8 ,  0 ), /* A9 */
			new OpCode( "RES 5,D"           ,  8 ,  0 ), /* AA */
			new OpCode( "RES 5,E"           ,  8 ,  0 ), /* AB */
			new OpCode( "RES 5,H"           ,  8 ,  0 ), /* AC */
			new OpCode( "RES 5,L"           ,  8 ,  0 ), /* AD */
			new OpCode( "RES 5,(HL)"        , 15 ,  0 ), /* AE */
			new OpCode( "RES 5,A"           ,  8 ,  0 ), /* AF */
			new OpCode( "RES 6,B"           ,  8 ,  0 ), /* B0 */
			new OpCode( "RES 6,C"           ,  8 ,  0 ), /* B1 */
			new OpCode( "RES 6,D"           ,  8 ,  0 ), /* B2 */
			new OpCode( "RES 6,E"           ,  8 ,  0 ), /* B3 */
			new OpCode( "RES 6,H"           ,  8 ,  0 ), /* B4 */
			new OpCode( "RES 6,L"           ,  8 ,  0 ), /* B5 */
			new OpCode( "RES 6,(HL)"        , 15 ,  0 ), /* B6 */
			new OpCode( "RES 6,A"           ,  8 ,  0 ), /* B7 */
			new OpCode( "RES 7,B"           ,  8 ,  0 ), /* B8 */
			new OpCode( "RES 7,C"           ,  8 ,  0 ), /* B9 */
			new OpCode( "RES 7,D"           ,  8 ,  0 ), /* BA */
			new OpCode( "RES 7,E"           ,  8 ,  0 ), /* BB */
			new OpCode( "RES 7,H"           ,  8 ,  0 ), /* BC */
			new OpCode( "RES 7,L"           ,  8 ,  0 ), /* BD */
			new OpCode( "RES 7,(HL)"        , 15 ,  0 ), /* BE */
			new OpCode( "RES 7,A"           ,  8 ,  0 ), /* BF */
			new OpCode( "SET 0,B"           ,  8 ,  0 ), /* C0 */
			new OpCode( "SET 0,C"           ,  8 ,  0 ), /* C1 */
			new OpCode( "SET 0,D"           ,  8 ,  0 ), /* C2 */
			new OpCode( "SET 0,E"           ,  8 ,  0 ), /* C3 */
			new OpCode( "SET 0,H"           ,  8 ,  0 ), /* C4 */
			new OpCode( "SET 0,L"           ,  8 ,  0 ), /* C5 */
			new OpCode( "SET 0,(HL)"        , 15 ,  0 ), /* C6 */
			new OpCode( "SET 0,A"           ,  8 ,  0 ), /* C7 */
			new OpCode( "SET 1,B"           ,  8 ,  0 ), /* C8 */
			new OpCode( "SET 1,C"           ,  8 ,  0 ), /* C9 */
			new OpCode( "SET 1,D"           ,  8 ,  0 ), /* CA */
			new OpCode( "SET 1,E"           ,  8 ,  0 ), /* CB */
			new OpCode( "SET 1,H"           ,  8 ,  0 ), /* CC */
			new OpCode( "SET 1,L"           ,  8 ,  0 ), /* CD */
			new OpCode( "SET 1,(HL)"        , 15 ,  0 ), /* CE */
			new OpCode( "SET 1,A"           ,  8 ,  0 ), /* CF */
			new OpCode( "SET 2,B"           ,  8 ,  0 ), /* D0 */
			new OpCode( "SET 2,C"           ,  8 ,  0 ), /* D1 */
			new OpCode( "SET 2,D"           ,  8 ,  0 ), /* D2 */
			new OpCode( "SET 2,E"           ,  8 ,  0 ), /* D3 */
			new OpCode( "SET 2,H"           ,  8 ,  0 ), /* D4 */
			new OpCode( "SET 2,L"           ,  8 ,  0 ), /* D5 */
			new OpCode( "SET 2,(HL)"        , 15 ,  0 ), /* D6 */
			new OpCode( "SET 2,A"           ,  8 ,  0 ), /* D7 */
			new OpCode( "SET 3,B"           ,  8 ,  0 ), /* D8 */
			new OpCode( "SET 3,C"           ,  8 ,  0 ), /* D9 */
			new OpCode( "SET 3,D"           ,  8 ,  0 ), /* DA */
			new OpCode( "SET 3,E"           ,  8 ,  0 ), /* DB */
			new OpCode( "SET 3,H"           ,  8 ,  0 ), /* DC */
			new OpCode( "SET 3,L"           ,  8 ,  0 ), /* DD */
			new OpCode( "SET 3,(HL)"        , 15 ,  0 ), /* DE */
			new OpCode( "SET 3,A"           ,  8 ,  0 ), /* DF */
			new OpCode( "SET 4,B"           ,  8 ,  0 ), /* E0 */
			new OpCode( "SET 4,C"           ,  8 ,  0 ), /* E1 */
			new OpCode( "SET 4,D"           ,  8 ,  0 ), /* E2 */
			new OpCode( "SET 4,E"           ,  8 ,  0 ), /* E3 */
			new OpCode( "SET 4,H"           ,  8 ,  0 ), /* E4 */
			new OpCode( "SET 4,L"           ,  8 ,  0 ), /* E5 */
			new OpCode( "SET 4,(HL)"        , 15 ,  0 ), /* E6 */
			new OpCode( "SET 4,A"           ,  8 ,  0 ), /* E7 */
			new OpCode( "SET 5,B"           ,  8 ,  0 ), /* E8 */
			new OpCode( "SET 5,C"           ,  8 ,  0 ), /* E9 */
			new OpCode( "SET 5,D"           ,  8 ,  0 ), /* EA */
			new OpCode( "SET 5,E"           ,  8 ,  0 ), /* EB */
			new OpCode( "SET 5,H"           ,  8 ,  0 ), /* EC */
			new OpCode( "SET 5,L"           ,  8 ,  0 ), /* ED */
			new OpCode( "SET 5,(HL)"        , 15 ,  0 ), /* EE */
			new OpCode( "SET 5,A"           ,  8 ,  0 ), /* EF */
			new OpCode( "SET 6,B"           ,  8 ,  0 ), /* F0 */
			new OpCode( "SET 6,C"           ,  8 ,  0 ), /* F1 */
			new OpCode( "SET 6,D"           ,  8 ,  0 ), /* F2 */
			new OpCode( "SET 6,E"           ,  8 ,  0 ), /* F3 */
			new OpCode( "SET 6,H"           ,  8 ,  0 ), /* F4 */
			new OpCode( "SET 6,L"           ,  8 ,  0 ), /* F5 */
			new OpCode( "SET 6,(HL)"        , 15 ,  0 ), /* F6 */
			new OpCode( "SET 6,A"           ,  8 ,  0 ), /* F7 */
			new OpCode( "SET 7,B"           ,  8 ,  0 ), /* F8 */
			new OpCode( "SET 7,C"           ,  8 ,  0 ), /* F9 */
			new OpCode( "SET 7,D"           ,  8 ,  0 ), /* FA */
			new OpCode( "SET 7,E"           ,  8 ,  0 ), /* FB */
			new OpCode( "SET 7,H"           ,  8 ,  0 ), /* FC */
			new OpCode( "SET 7,L"           ,  8 ,  0 ), /* FD */
			new OpCode( "SET 7,(HL)"        , 15 ,  0 ), /* FE */
			new OpCode( "SET 7,A"           ,  8 ,  0 ), /* FF */

		};


		/**/
		public static OpCode[] dasm_ed = new OpCode[]
		{
			new OpCode( null                ,  0 ,  0 ), /* 00 */
			new OpCode( null                ,  0 ,  0 ), /* 01 */
			new OpCode( null                ,  0 ,  0 ), /* 02 */
			new OpCode( null                ,  0 ,  0 ), /* 03 */
			new OpCode( null                ,  0 ,  0 ), /* 04 */
			new OpCode( null                ,  0 ,  0 ), /* 05 */
			new OpCode( null                ,  0 ,  0 ), /* 06 */
			new OpCode( null                ,  0 ,  0 ), /* 07 */
			new OpCode( null                ,  0 ,  0 ), /* 08 */
			new OpCode( null                ,  0 ,  0 ), /* 09 */
			new OpCode( null                ,  0 ,  0 ), /* 0A */
			new OpCode( null                ,  0 ,  0 ), /* 0B */
			new OpCode( null                ,  0 ,  0 ), /* 0C */
			new OpCode( null                ,  0 ,  0 ), /* 0D */
			new OpCode( null                ,  0 ,  0 ), /* 0E */
			new OpCode( null                ,  0 ,  0 ), /* 0F */
			new OpCode( null                ,  0 ,  0 ), /* 10 */
			new OpCode( null                ,  0 ,  0 ), /* 11 */
			new OpCode( null                ,  0 ,  0 ), /* 12 */
			new OpCode( null                ,  0 ,  0 ), /* 13 */
			new OpCode( null                ,  0 ,  0 ), /* 14 */
			new OpCode( null                ,  0 ,  0 ), /* 15 */
			new OpCode( null                ,  0 ,  0 ), /* 16 */
			new OpCode( null                ,  0 ,  0 ), /* 17 */
			new OpCode( null                ,  0 ,  0 ), /* 18 */
			new OpCode( null                ,  0 ,  0 ), /* 19 */
			new OpCode( null                ,  0 ,  0 ), /* 1A */
			new OpCode( null                ,  0 ,  0 ), /* 1B */
			new OpCode( null                ,  0 ,  0 ), /* 1C */
			new OpCode( null                ,  0 ,  0 ), /* 1D */
			new OpCode( null                ,  0 ,  0 ), /* 1E */
			new OpCode( null                ,  0 ,  0 ), /* 1F */
			new OpCode( null                ,  0 ,  0 ), /* 20 */
			new OpCode( null                ,  0 ,  0 ), /* 21 */
			new OpCode( null                ,  0 ,  0 ), /* 22 */
			new OpCode( null                ,  0 ,  0 ), /* 23 */
			new OpCode( null                ,  0 ,  0 ), /* 24 */
			new OpCode( null                ,  0 ,  0 ), /* 25 */
			new OpCode( null                ,  0 ,  0 ), /* 26 */
			new OpCode( null                ,  0 ,  0 ), /* 27 */
			new OpCode( null                ,  0 ,  0 ), /* 28 */
			new OpCode( null                ,  0 ,  0 ), /* 29 */
			new OpCode( null                ,  0 ,  0 ), /* 2A */
			new OpCode( null                ,  0 ,  0 ), /* 2B */
			new OpCode( null                ,  0 ,  0 ), /* 2C */
			new OpCode( null                ,  0 ,  0 ), /* 2D */
			new OpCode( null                ,  0 ,  0 ), /* 2E */
			new OpCode( null                ,  0 ,  0 ), /* 2F */
			new OpCode( null                ,  0 ,  0 ), /* 30 */
			new OpCode( null                ,  0 ,  0 ), /* 31 */
			new OpCode( null                ,  0 ,  0 ), /* 32 */
			new OpCode( null                ,  0 ,  0 ), /* 33 */
			new OpCode( null                ,  0 ,  0 ), /* 34 */
			new OpCode( null                ,  0 ,  0 ), /* 35 */
			new OpCode( null                ,  0 ,  0 ), /* 36 */
			new OpCode( null                ,  0 ,  0 ), /* 37 */
			new OpCode( null                ,  0 ,  0 ), /* 38 */
			new OpCode( null                ,  0 ,  0 ), /* 39 */
			new OpCode( null                ,  0 ,  0 ), /* 3A */
			new OpCode( null                ,  0 ,  0 ), /* 3B */
			new OpCode( null                ,  0 ,  0 ), /* 3C */
			new OpCode( null                ,  0 ,  0 ), /* 3D */
			new OpCode( null                ,  0 ,  0 ), /* 3E */
			new OpCode( null                ,  0 ,  0 ), /* 3F */
			new OpCode( "IN B,(C)"          , 12 ,  0 ), /* 40 */
			new OpCode( "OUT (C),B"         , 12 ,  0 ), /* 41 */
			new OpCode( "SBC HL,BC"         , 15 ,  0 ), /* 42 */
			new OpCode( "LD (@),BC"         , 20 ,  0 ), /* 43 */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 44 */
			new OpCode( "RETN"              , 14 ,  0 , OpCodeFlags.Returns), /* 45 */
			new OpCode( "IM 0"              ,  8 ,  0 ), /* 46 */
			new OpCode( "LD I,A"            ,  9 ,  0 ), /* 47 */
			new OpCode( "IN C,(C)"          , 12 ,  0 ), /* 48 */
			new OpCode( "OUT (C),C"         , 12 ,  0 ), /* 49 */
			new OpCode( "ADC HL,BC"         , 15 ,  0 ), /* 4A */
			new OpCode( "LD BC,(@)"         , 20 ,  0 ), /* 4B */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 4C */
			new OpCode( "RETI"              , 14 ,  0 , OpCodeFlags.Returns), /* 4D */
			new OpCode( "IM 0"              ,  8 ,  0 ), /* 4E */
			new OpCode( "LD_R_A"            ,  9 ,  0 ), /* 4F */
			new OpCode( "IN D,(C)"          , 12 ,  0 ), /* 50 */
			new OpCode( "OUT (C),D"         , 12 ,  0 ), /* 51 */
			new OpCode( "SBC HL,DE"         , 15 ,  0 ), /* 52 */
			new OpCode( "LD (@),DE"         , 20 ,  0 ), /* 53 */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 54 */
			new OpCode( "RETN"              , 14 ,  0 , OpCodeFlags.Returns), /* 55 */
			new OpCode( "IM 1"              ,  8 ,  0 ), /* 56 */
			new OpCode( "LD A,I"            ,  9 ,  0 ), /* 57 */
			new OpCode( "IN E,(C)"          , 12 ,  0 ), /* 58 */
			new OpCode( "OUT (C),E"         , 12 ,  0 ), /* 59 */
			new OpCode( "ADC HL,DE"         , 15 ,  0 ), /* 5A */
			new OpCode( "LD DE,(@)"         , 20 ,  0 ), /* 5B */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 5C */
			new OpCode( "RETI"              , 14 ,  0 , OpCodeFlags.Returns), /* 5D */
			new OpCode( "IM 2"              ,  8 ,  0 ), /* 5E */
			new OpCode( "LD A,R"            ,  9 ,  0 ), /* 5F */
			new OpCode( "IN H,(C)"          , 12 ,  0 ), /* 60 */
			new OpCode( "OUT (C),H"         , 12 ,  0 ), /* 61 */
			new OpCode( "SBC HL,HL"         , 15 ,  0 ), /* 62 */
			new OpCode( "LD (@),HL"         , 20 ,  0 ), /* 63 */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 64 */
			new OpCode( "RETN"              , 14 ,  0 , OpCodeFlags.Returns), /* 65 */
			new OpCode( "IM 0"              ,  8 ,  0 ), /* 66 */
			new OpCode( "RRD"               , 18 ,  0 ), /* 67 */
			new OpCode( "IN L,(C)"          , 12 ,  0 ), /* 68 */
			new OpCode( "OUT (C),L"         , 12 ,  0 ), /* 69 */
			new OpCode( "ADC HL,HL"         , 15 ,  0 ), /* 6A */
			new OpCode( "LD HL,(@)"         , 20 ,  0 ), /* 6B */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 6C */
			new OpCode( "RETI"              , 14 ,  0 , OpCodeFlags.Returns ), /* 6D */
			new OpCode( "IM 0"              ,  8 ,  0 ), /* 6E */
			new OpCode( "RLD"               , 18 ,  0 ), /* 6F */
			new OpCode( "IN_F (C)"          , 12 ,  0 ), /* 70 */
			new OpCode( "OUT (C),0"         , 12 ,  0 ), /* 71 */
			new OpCode( "SBC HL,SP"         , 15 ,  0 ), /* 72 */
			new OpCode( "LD (@),SP"         , 20 ,  0 ), /* 73 */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 74 */
			new OpCode( "RETN"              , 14 ,  0 , OpCodeFlags.Returns ), /* 75 */
			new OpCode( "IM 1"              ,  8 ,  0 ), /* 76 */
			new OpCode( null                ,  0 ,  0 ), /* 77 */
			new OpCode( "IN A,(C)"          , 12 ,  0 ), /* 78 */
			new OpCode( "OUT (C),A"         , 12 ,  0 ), /* 79 */
			new OpCode( "ADC HL,SP"         , 15 ,  0 ), /* 7A */
			new OpCode( "LD SP,(@)"         , 20 ,  0 ), /* 7B */
			new OpCode( "NEG"               ,  8 ,  0 ), /* 7C */
			new OpCode( "RETI"              , 14 ,  0 , OpCodeFlags.Returns ), /* 7D */
			new OpCode( "IM 2"              ,  8 ,  0 ), /* 7E */
			new OpCode( null                ,  0 ,  0 ), /* 7F */
			new OpCode( null                ,  0 ,  0 ), /* 80 */
			new OpCode( null                ,  0 ,  0 ), /* 81 */
			new OpCode( null                ,  0 ,  0 ), /* 82 */
			new OpCode( null                ,  0 ,  0 ), /* 83 */
			new OpCode( null                ,  0 ,  0 ), /* 84 */
			new OpCode( null                ,  0 ,  0 ), /* 85 */
			new OpCode( null                ,  0 ,  0 ), /* 86 */
			new OpCode( null                ,  0 ,  0 ), /* 87 */
			new OpCode( null                ,  0 ,  0 ), /* 88 */
			new OpCode( null                ,  0 ,  0 ), /* 89 */
			new OpCode( null                ,  0 ,  0 ), /* 8A */
			new OpCode( null                ,  0 ,  0 ), /* 8B */
			new OpCode( null                ,  0 ,  0 ), /* 8C */
			new OpCode( null                ,  0 ,  0 ), /* 8D */
			new OpCode( null                ,  0 ,  0 ), /* 8E */
			new OpCode( null                ,  0 ,  0 ), /* 8F */
			new OpCode( null                ,  0 ,  0 ), /* 90 */
			new OpCode( null                ,  0 ,  0 ), /* 91 */
			new OpCode( null                ,  0 ,  0 ), /* 92 */
			new OpCode( null                ,  0 ,  0 ), /* 93 */
			new OpCode( null                ,  0 ,  0 ), /* 94 */
			new OpCode( null                ,  0 ,  0 ), /* 95 */
			new OpCode( null                ,  0 ,  0 ), /* 96 */
			new OpCode( null                ,  0 ,  0 ), /* 97 */
			new OpCode( null                ,  0 ,  0 ), /* 98 */
			new OpCode( null                ,  0 ,  0 ), /* 99 */
			new OpCode( null                ,  0 ,  0 ), /* 9A */
			new OpCode( null                ,  0 ,  0 ), /* 9B */
			new OpCode( null                ,  0 ,  0 ), /* 9C */
			new OpCode( null                ,  0 ,  0 ), /* 9D */
			new OpCode( null                ,  0 ,  0 ), /* 9E */
			new OpCode( null                ,  0 ,  0 ), /* 9F */
			new OpCode( "LDI"               , 16 ,  0 ), /* A0 */
			new OpCode( "CPI"               , 16 ,  0 ), /* A1 */
			new OpCode( "INI"               , 16 ,  0 ), /* A2 */
			new OpCode( "OUTI"              , 16 ,  0 ), /* A3 */
			new OpCode( null                ,  0 ,  0 ), /* A4 */
			new OpCode( null                ,  0 ,  0 ), /* A5 */
			new OpCode( null                ,  0 ,  0 ), /* A6 */
			new OpCode( null                ,  0 ,  0 ), /* A7 */
			new OpCode( "LDD"               , 16 ,  0 ), /* A8 */
			new OpCode( "CPD"               , 16 ,  0 ), /* A9 */
			new OpCode( "IND"               , 16 ,  0 ), /* AA */
			new OpCode( "OUTD"              , 16 ,  0 ), /* AB */
			new OpCode( null                ,  0 ,  0 ), /* AC */
			new OpCode( null                ,  0 ,  0 ), /* AD */
			new OpCode( null                ,  0 ,  0 ), /* AE */
			new OpCode( null                ,  0 ,  0 ), /* AF */
			new OpCode( "LDIR"              , 16 , 21 ), /* B0 */
			new OpCode( "CPIR"              , 16 , 21 ), /* B1 */
			new OpCode( "INIR"              , 16 , 21 ), /* B2 */
			new OpCode( "OTIR"              , 16 , 21 ), /* B3 */
			new OpCode( null                ,  0 ,  0 ), /* B4 */
			new OpCode( null                ,  0 ,  0 ), /* B5 */
			new OpCode( null                ,  0 ,  0 ), /* B6 */
			new OpCode( null                ,  0 ,  0 ), /* B7 */
			new OpCode( "LDDR"              , 16 , 21 ), /* B8 */
			new OpCode( "CPDR"              , 16 , 21 ), /* B9 */
			new OpCode( "INDR"              , 16 , 21 ), /* BA */
			new OpCode( "OTDR"              , 16 , 21 ), /* BB */
			new OpCode( null                ,  0 ,  0 ), /* BC */
			new OpCode( null                ,  0 ,  0 ), /* BD */
			new OpCode( null                ,  0 ,  0 ), /* BE */
			new OpCode( null                ,  0 ,  0 ), /* BF */
			new OpCode( null                ,  0 ,  0 ), /* C0 */
			new OpCode( null                ,  0 ,  0 ), /* C1 */
			new OpCode( null                ,  0 ,  0 ), /* C2 */
			new OpCode( null                ,  0 ,  0 ), /* C3 */
			new OpCode( null                ,  0 ,  0 ), /* C4 */
			new OpCode( null                ,  0 ,  0 ), /* C5 */
			new OpCode( null                ,  0 ,  0 ), /* C6 */
			new OpCode( null                ,  0 ,  0 ), /* C7 */
			new OpCode( null                ,  0 ,  0 ), /* C8 */
			new OpCode( null                ,  0 ,  0 ), /* C9 */
			new OpCode( null                ,  0 ,  0 ), /* CA */
			new OpCode( null                ,  0 ,  0 ), /* CB */
			new OpCode( null                ,  0 ,  0 ), /* CC */
			new OpCode( null                ,  0 ,  0 ), /* CD */
			new OpCode( null                ,  0 ,  0 ), /* CE */
			new OpCode( null                ,  0 ,  0 ), /* CF */
			new OpCode( null                ,  0 ,  0 ), /* D0 */
			new OpCode( null                ,  0 ,  0 ), /* D1 */
			new OpCode( null                ,  0 ,  0 ), /* D2 */
			new OpCode( null                ,  0 ,  0 ), /* D3 */
			new OpCode( null                ,  0 ,  0 ), /* D4 */
			new OpCode( null                ,  0 ,  0 ), /* D5 */
			new OpCode( null                ,  0 ,  0 ), /* D6 */
			new OpCode( null                ,  0 ,  0 ), /* D7 */
			new OpCode( null                ,  0 ,  0 ), /* D8 */
			new OpCode( null                ,  0 ,  0 ), /* D9 */
			new OpCode( null                ,  0 ,  0 ), /* DA */
			new OpCode( null                ,  0 ,  0 ), /* DB */
			new OpCode( null                ,  0 ,  0 ), /* DC */
			new OpCode( null                ,  0 ,  0 ), /* DD */
			new OpCode( null                ,  0 ,  0 ), /* DE */
			new OpCode( null                ,  0 ,  0 ), /* DF */
			new OpCode( null                ,  0 ,  0 ), /* E0 */
			new OpCode( null                ,  0 ,  0 ), /* E1 */
			new OpCode( null                ,  0 ,  0 ), /* E2 */
			new OpCode( null                ,  0 ,  0 ), /* E3 */
			new OpCode( null                ,  0 ,  0 ), /* E4 */
			new OpCode( null                ,  0 ,  0 ), /* E5 */
			new OpCode( null                ,  0 ,  0 ), /* E6 */
			new OpCode( null                ,  0 ,  0 ), /* E7 */
			new OpCode( null                ,  0 ,  0 ), /* E8 */
			new OpCode( null                ,  0 ,  0 ), /* E9 */
			new OpCode( null                ,  0 ,  0 ), /* EA */
			new OpCode( null                ,  0 ,  0 ), /* EB */
			new OpCode( null                ,  0 ,  0 ), /* EC */
			new OpCode( null                ,  0 ,  0 ), /* ED */
			new OpCode( null                ,  0 ,  0 ), /* EE */
			new OpCode( null                ,  0 ,  0 ), /* EF */
			new OpCode( null                ,  0 ,  0 ), /* F0 */
			new OpCode( null                ,  0 ,  0 ), /* F1 */
			new OpCode( null                ,  0 ,  0 ), /* F2 */
			new OpCode( null                ,  0 ,  0 ), /* F3 */
			new OpCode( null                ,  0 ,  0 ), /* F4 */
			new OpCode( null                ,  0 ,  0 ), /* F5 */
			new OpCode( null                ,  0 ,  0 ), /* F6 */
			new OpCode( null                ,  0 ,  0 ), /* F7 */
			new OpCode( null                ,  0 ,  0 ), /* F8 */
			new OpCode( null                ,  0 ,  0 ), /* F9 */
			new OpCode( null                ,  0 ,  0 ), /* FA */
			new OpCode( null                ,  0 ,  0 ), /* FB */
			new OpCode( null                ,  0 ,  0 ), /* FC */
			new OpCode( null                ,  0 ,  0 ), /* FD */
			new OpCode( null                ,  0 ,  0 ), /* FE */
			new OpCode( null                ,  0 ,  0 ), /* FF */
		};


		/**/
		public static OpCode[] dasm_dd = new OpCode[]
		{
			new OpCode( null                ,  0 ,  0 ), /* 00 */
			new OpCode( null                ,  0 ,  0 ), /* 01 */
			new OpCode( null                ,  0 ,  0 ), /* 02 */
			new OpCode( null                ,  0 ,  0 ), /* 03 */
			new OpCode( null                ,  0 ,  0 ), /* 04 */
			new OpCode( null                ,  0 ,  0 ), /* 05 */
			new OpCode( null                ,  0 ,  0 ), /* 06 */
			new OpCode( null                ,  0 ,  0 ), /* 07 */
			new OpCode( null                ,  0 ,  0 ), /* 08 */
			new OpCode( "ADD IX,BC"         , 15 ,  0 ), /* 09 */
			new OpCode( null                ,  0 ,  0 ), /* 0A */
			new OpCode( null                ,  0 ,  0 ), /* 0B */
			new OpCode( null                ,  0 ,  0 ), /* 0C */
			new OpCode( null                ,  0 ,  0 ), /* 0D */
			new OpCode( null                ,  0 ,  0 ), /* 0E */
			new OpCode( null                ,  0 ,  0 ), /* 0F */
			new OpCode( null                ,  0 ,  0 ), /* 10 */
			new OpCode( null                ,  0 ,  0 ), /* 11 */
			new OpCode( null                ,  0 ,  0 ), /* 12 */
			new OpCode( null                ,  0 ,  0 ), /* 13 */
			new OpCode( null                ,  0 ,  0 ), /* 14 */
			new OpCode( null                ,  0 ,  0 ), /* 15 */
			new OpCode( null                ,  0 ,  0 ), /* 16 */
			new OpCode( null                ,  0 ,  0 ), /* 17 */
			new OpCode( null                ,  0 ,  0 ), /* 18 */
			new OpCode( "ADD IX,DE"         , 15 ,  0 ), /* 19 */
			new OpCode( null                ,  0 ,  0 ), /* 1A */
			new OpCode( null                ,  0 ,  0 ), /* 1B */
			new OpCode( null                ,  0 ,  0 ), /* 1C */
			new OpCode( null                ,  0 ,  0 ), /* 1D */
			new OpCode( null                ,  0 ,  0 ), /* 1E */
			new OpCode( null                ,  0 ,  0 ), /* 1F */
			new OpCode( null                ,  0 ,  0 ), /* 20 */
			new OpCode( "LD IX,@"           , 14 ,  0 ), /* 21 */
			new OpCode( "LD (@),IX"         , 20 ,  0 ), /* 22 */
			new OpCode( "INC IX"            , 10 ,  0 ), /* 23 */
			new OpCode( "INC IXH"           ,  8 ,  0 ), /* 24 */
			new OpCode( "DEC IXH"           ,  8 ,  0 ), /* 25 */
			new OpCode( "LD IXH,#"          , 11 ,  0 ), /* 26 */
			new OpCode( null                ,  0 ,  0 ), /* 27 */
			new OpCode( null                ,  0 ,  0 ), /* 28 */
			new OpCode( "ADD IX,IX"         , 15 ,  0 ), /* 29 */
			new OpCode( "LD IX,(@)"         , 20 ,  0 ), /* 2A */
			new OpCode( "DEC IX"            , 10 ,  0 ), /* 2B */
			new OpCode( "INC IXL"           ,  8 ,  0 ), /* 2C */
			new OpCode( "DEC IXL"           ,  8 ,  0 ), /* 2D */
			new OpCode( "LD IXL,#"          , 11 ,  0 ), /* 2E */
			new OpCode( null                ,  0 ,  0 ), /* 2F */
			new OpCode( null                ,  0 ,  0 ), /* 30 */
			new OpCode( null                ,  0 ,  0 ), /* 31 */
			new OpCode( null                ,  0 ,  0 ), /* 32 */
			new OpCode( null                ,  0 ,  0 ), /* 33 */
			new OpCode( "INC (IX+$)"        , 23 ,  0 ), /* 34 */
			new OpCode( "DEC (IX+$)"        , 23 ,  0 ), /* 35 */
			new OpCode( "LD (IX+$),#"       , 19 ,  0 ), /* 36 */
			new OpCode( null                ,  0 ,  0 ), /* 37 */
			new OpCode( null                ,  0 ,  0 ), /* 38 */
			new OpCode( "ADD IX,SP"         , 15 ,  0 ), /* 39 */
			new OpCode( null                ,  0 ,  0 ), /* 3A */
			new OpCode( null                ,  0 ,  0 ), /* 3B */
			new OpCode( null                ,  0 ,  0 ), /* 3C */
			new OpCode( null                ,  0 ,  0 ), /* 3D */
			new OpCode( null                ,  0 ,  0 ), /* 3E */
			new OpCode( null                ,  0 ,  0 ), /* 3F */
			new OpCode( null                ,  0 ,  0 ), /* 40 */
			new OpCode( null                ,  0 ,  0 ), /* 41 */
			new OpCode( null                ,  0 ,  0 ), /* 42 */
			new OpCode( null                ,  0 ,  0 ), /* 43 */
			new OpCode( "LD B,IXH"          ,  8 ,  0 ), /* 44 */
			new OpCode( "LD B,IXL"          ,  8 ,  0 ), /* 45 */
			new OpCode( "LD B,(IX+$)"       , 19 ,  0 ), /* 46 */
			new OpCode( null                ,  0 ,  0 ), /* 47 */
			new OpCode( null                ,  0 ,  0 ), /* 48 */
			new OpCode( null                ,  0 ,  0 ), /* 49 */
			new OpCode( null                ,  0 ,  0 ), /* 4A */
			new OpCode( null                ,  0 ,  0 ), /* 4B */
			new OpCode( "LD C,IXH"          ,  8 ,  0 ), /* 4C */
			new OpCode( "LD C,IXL"          ,  8 ,  0 ), /* 4D */
			new OpCode( "LD C,(IX+$)"       , 19 ,  0 ), /* 4E */
			new OpCode( null                ,  0 ,  0 ), /* 4F */
			new OpCode( null                ,  0 ,  0 ), /* 50 */
			new OpCode( null                ,  0 ,  0 ), /* 51 */
			new OpCode( null                ,  0 ,  0 ), /* 52 */
			new OpCode( null                ,  0 ,  0 ), /* 53 */
			new OpCode( "LD D,IXH"          ,  8 ,  0 ), /* 54 */
			new OpCode( "LD D,IXL"          ,  8 ,  0 ), /* 55 */
			new OpCode( "LD D,(IX+$)"       , 19 ,  0 ), /* 56 */
			new OpCode( null                ,  0 ,  0 ), /* 57 */
			new OpCode( null                ,  0 ,  0 ), /* 58 */
			new OpCode( null                ,  0 ,  0 ), /* 59 */
			new OpCode( null                ,  0 ,  0 ), /* 5A */
			new OpCode( null                ,  0 ,  0 ), /* 5B */
			new OpCode( "LD E,IXH"          ,  8 ,  0 ), /* 5C */
			new OpCode( "LD E,IXL"          ,  8 ,  0 ), /* 5D */
			new OpCode( "LD E,(IX+$)"       , 19 ,  0 ), /* 5E */
			new OpCode( null                ,  0 ,  0 ), /* 5F */
			new OpCode( "LD IXH,B"          ,  8 ,  0 ), /* 60 */
			new OpCode( "LD IXH,C"          ,  8 ,  0 ), /* 61 */
			new OpCode( "LD IXH,D"          ,  8 ,  0 ), /* 62 */
			new OpCode( "LD IXH,E"          ,  8 ,  0 ), /* 63 */
			new OpCode( "LD IXH,IXH"        ,  8 ,  0 ), /* 64 */
			new OpCode( "LD IXH,IXL"        ,  8 ,  0 ), /* 65 */
			new OpCode( "LD H,(IX+$)"       , 19 ,  0 ), /* 66 */
			new OpCode( "LD IXH,A"          ,  8 ,  0 ), /* 67 */
			new OpCode( "LD IXL,B"          ,  8 ,  0 ), /* 68 */
			new OpCode( "LD IXL,C"          ,  8 ,  0 ), /* 69 */
			new OpCode( "LD IXL,D"          ,  8 ,  0 ), /* 6A */
			new OpCode( "LD IXL,E"          ,  8 ,  0 ), /* 6B */
			new OpCode( "LD IXL,IXH"        ,  8 ,  0 ), /* 6C */
			new OpCode( "LD IXL,IXL"        ,  8 ,  0 ), /* 6D */
			new OpCode( "LD L,(IX+$)"       , 19 ,  0 ), /* 6E */
			new OpCode( "LD IXL,A"          ,  8 ,  0 ), /* 6F */
			new OpCode( "LD (IX+$),B"       , 19 ,  0 ), /* 70 */
			new OpCode( "LD (IX+$),C"       , 19 ,  0 ), /* 71 */
			new OpCode( "LD (IX+$),D"       , 19 ,  0 ), /* 72 */
			new OpCode( "LD (IX+$),E"       , 19 ,  0 ), /* 73 */
			new OpCode( "LD (IX+$),H"       , 19 ,  0 ), /* 74 */
			new OpCode( "LD (IX+$),L"       , 19 ,  0 ), /* 75 */
			new OpCode( null                ,  0 ,  0 ), /* 76 */
			new OpCode( "LD (IX+$),A"       , 19 ,  0 ), /* 77 */
			new OpCode( null                ,  0 ,  0 ), /* 78 */
			new OpCode( null                ,  0 ,  0 ), /* 79 */
			new OpCode( null                ,  0 ,  0 ), /* 7A */
			new OpCode( null                ,  0 ,  0 ), /* 7B */
			new OpCode( "LD A,IXH"          ,  8 ,  0 ), /* 7C */
			new OpCode( "LD A,IXL"          ,  8 ,  0 ), /* 7D */
			new OpCode( "LD A,(IX+$)"       , 19 ,  0 ), /* 7E */
			new OpCode( null                ,  0 ,  0 ), /* 7F */
			new OpCode( null                ,  0 ,  0 ), /* 80 */
			new OpCode( null                ,  0 ,  0 ), /* 81 */
			new OpCode( null                ,  0 ,  0 ), /* 82 */
			new OpCode( null                ,  0 ,  0 ), /* 83 */
			new OpCode( "ADD A,IXH"         ,  8 ,  0 ), /* 84 */
			new OpCode( "ADD A,IXL"         ,  8 ,  0 ), /* 85 */
			new OpCode( "ADD A,(IX+$)"      , 19 ,  0 ), /* 86 */
			new OpCode( null                ,  0 ,  0 ), /* 87 */
			new OpCode( null                ,  0 ,  0 ), /* 88 */
			new OpCode( null                ,  0 ,  0 ), /* 89 */
			new OpCode( null                ,  0 ,  0 ), /* 8A */
			new OpCode( null                ,  0 ,  0 ), /* 8B */
			new OpCode( "ADC A,IXH"         ,  8 ,  0 ), /* 8C */
			new OpCode( "ADC A,IXL"         ,  8 ,  0 ), /* 8D */
			new OpCode( "ADC A,(IX+$)"      , 19 ,  0 ), /* 8E */
			new OpCode( null                ,  0 ,  0 ), /* 8F */
			new OpCode( null                ,  0 ,  0 ), /* 90 */
			new OpCode( null                ,  0 ,  0 ), /* 91 */
			new OpCode( null                ,  0 ,  0 ), /* 92 */
			new OpCode( null                ,  0 ,  0 ), /* 93 */
			new OpCode( "SUB IXH"           ,  8 ,  0 ), /* 94 */
			new OpCode( "SUB IXL"           ,  8 ,  0 ), /* 95 */
			new OpCode( "SUB (IX+$)"        , 19 ,  0 ), /* 96 */
			new OpCode( null                ,  0 ,  0 ), /* 97 */
			new OpCode( null                ,  0 ,  0 ), /* 98 */
			new OpCode( null                ,  0 ,  0 ), /* 99 */
			new OpCode( null                ,  0 ,  0 ), /* 9A */
			new OpCode( null                ,  0 ,  0 ), /* 9B */
			new OpCode( "SBC A,IXH"         ,  8 ,  0 ), /* 9C */
			new OpCode( "SBC A,IXL"         ,  8 ,  0 ), /* 9D */
			new OpCode( "SBC A,(IX+$)"      , 19 ,  0 ), /* 9E */
			new OpCode( null                ,  0 ,  0 ), /* 9F */
			new OpCode( null                ,  0 ,  0 ), /* A0 */
			new OpCode( null                ,  0 ,  0 ), /* A1 */
			new OpCode( null                ,  0 ,  0 ), /* A2 */
			new OpCode( null                ,  0 ,  0 ), /* A3 */
			new OpCode( "AND IXH"           ,  8 ,  0 ), /* A4 */
			new OpCode( "AND IXL"           ,  8 ,  0 ), /* A5 */
			new OpCode( "AND (IX+$)"        , 19 ,  0 ), /* A6 */
			new OpCode( null                ,  0 ,  0 ), /* A7 */
			new OpCode( null                ,  0 ,  0 ), /* A8 */
			new OpCode( null                ,  0 ,  0 ), /* A9 */
			new OpCode( null                ,  0 ,  0 ), /* AA */
			new OpCode( null                ,  0 ,  0 ), /* AB */
			new OpCode( "XOR IXH"           ,  8 ,  0 ), /* AC */
			new OpCode( "XOR IXL"           ,  8 ,  0 ), /* AD */
			new OpCode( "XOR (IX+$)"        , 19 ,  0 ), /* AE */
			new OpCode( null                ,  0 ,  0 ), /* AF */
			new OpCode( null                ,  0 ,  0 ), /* B0 */
			new OpCode( null                ,  0 ,  0 ), /* B1 */
			new OpCode( null                ,  0 ,  0 ), /* B2 */
			new OpCode( null                ,  0 ,  0 ), /* B3 */
			new OpCode( "OR IXH"            ,  8 ,  0 ), /* B4 */
			new OpCode( "OR IXL"            ,  8 ,  0 ), /* B5 */
			new OpCode( "OR (IX+$)"         , 19 ,  0 ), /* B6 */
			new OpCode( null                ,  0 ,  0 ), /* B7 */
			new OpCode( null                ,  0 ,  0 ), /* B8 */
			new OpCode( null                ,  0 ,  0 ), /* B9 */
			new OpCode( null                ,  0 ,  0 ), /* BA */
			new OpCode( null                ,  0 ,  0 ), /* BB */
			new OpCode( "CP IXH"            ,  8 ,  0 ), /* BC */
			new OpCode( "CP IXL"            ,  8 ,  0 ), /* BD */
			new OpCode( "CP (IX+$)"         , 19 ,  0 ), /* BE */
			new OpCode( null                ,  0 ,  0 ), /* BF */
			new OpCode( null                ,  0 ,  0 ), /* C0 */
			new OpCode( null                ,  0 ,  0 ), /* C1 */
			new OpCode( null                ,  0 ,  0 ), /* C2 */
			new OpCode( null                ,  0 ,  0 ), /* C3 */
			new OpCode( null                ,  0 ,  0 ), /* C4 */
			new OpCode( null                ,  0 ,  0 ), /* C5 */
			new OpCode( null                ,  0 ,  0 ), /* C6 */
			new OpCode( null                ,  0 ,  0 ), /* C7 */
			new OpCode( null                ,  0 ,  0 ), /* C8 */
			new OpCode( null                ,  0 ,  0 ), /* C9 */
			new OpCode( null                ,  0 ,  0 ), /* CA */
			new OpCode( "shift CB"          ,  0 ,  0 ), /* CB */
			new OpCode( null                ,  0 ,  0 ), /* CC */
			new OpCode( null                ,  0 ,  0 ), /* CD */
			new OpCode( null                ,  0 ,  0 ), /* CE */
			new OpCode( null                ,  0 ,  0 ), /* CF */
			new OpCode( null                ,  0 ,  0 ), /* D0 */
			new OpCode( null                ,  0 ,  0 ), /* D1 */
			new OpCode( null                ,  0 ,  0 ), /* D2 */
			new OpCode( null                ,  0 ,  0 ), /* D3 */
			new OpCode( null                ,  0 ,  0 ), /* D4 */
			new OpCode( null                ,  0 ,  0 ), /* D5 */
			new OpCode( null                ,  0 ,  0 ), /* D6 */
			new OpCode( null                ,  0 ,  0 ), /* D7 */
			new OpCode( null                ,  0 ,  0 ), /* D8 */
			new OpCode( null                ,  0 ,  0 ), /* D9 */
			new OpCode( null                ,  0 ,  0 ), /* DA */
			new OpCode( null                ,  0 ,  0 ), /* DB */
			new OpCode( null                ,  0 ,  0 ), /* DC */
			new OpCode( "ignore"            ,  4 ,  0 ), /* DD */
			new OpCode( null                ,  0 ,  0 ), /* DE */
			new OpCode( null                ,  0 ,  0 ), /* DF */
			new OpCode( null                ,  0 ,  0 ), /* E0 */
			new OpCode( "POP IX"            , 14 ,  0 ), /* E1 */
			new OpCode( null                ,  0 ,  0 ), /* E2 */
			new OpCode( "EX (SP),IX"        , 23 ,  0 ), /* E3 */
			new OpCode( null                ,  0 ,  0 ), /* E4 */
			new OpCode( "PUSH IX"           , 15 ,  0 ), /* E5 */
			new OpCode( null                ,  0 ,  0 ), /* E6 */
			new OpCode( null                ,  0 ,  0 ), /* E7 */
			new OpCode( null                ,  0 ,  0 ), /* E8 */
			new OpCode( "JP (IX)"             ,  8 ,  0 , OpCodeFlags.Jumps), /* E9 */
			new OpCode( null                ,  0 ,  0 ), /* EA */
			new OpCode( null                ,  0 ,  0 ), /* EB */
			new OpCode( null                ,  0 ,  0 ), /* EC */
			new OpCode( null                ,  4 ,  0 ), /* ED */
			new OpCode( null                ,  0 ,  0 ), /* EE */
			new OpCode( null                ,  0 ,  0 ), /* EF */
			new OpCode( null                ,  0 ,  0 ), /* F0 */
			new OpCode( null                ,  0 ,  0 ), /* F1 */
			new OpCode( null                ,  0 ,  0 ), /* F2 */
			new OpCode( null                ,  0 ,  0 ), /* F3 */
			new OpCode( null                ,  0 ,  0 ), /* F4 */
			new OpCode( null                ,  0 ,  0 ), /* F5 */
			new OpCode( null                ,  0 ,  0 ), /* F6 */
			new OpCode( null                ,  0 ,  0 ), /* F7 */
			new OpCode( null                ,  0 ,  0 ), /* F8 */
			new OpCode( "LD SP,IX"          , 10 ,  0 ), /* F9 */
			new OpCode( null                ,  0 ,  0 ), /* FA */
			new OpCode( null                ,  0 ,  0 ), /* FB */
			new OpCode( null                ,  0 ,  0 ), /* FC */
			new OpCode( "ignore"            ,  4 ,  0 ), /* FD */
			new OpCode( null                ,  0 ,  0 ), /* FE */
			new OpCode( null                ,  0 ,  0 ), /* FF */
		};


		/**/
		public static OpCode[] dasm_fd = new OpCode[]
		{
			new OpCode( null                ,  0 ,  0 ), /* 00 */
			new OpCode( null                ,  0 ,  0 ), /* 01 */
			new OpCode( null                ,  0 ,  0 ), /* 02 */
			new OpCode( null                ,  0 ,  0 ), /* 03 */
			new OpCode( null                ,  0 ,  0 ), /* 04 */
			new OpCode( null                ,  0 ,  0 ), /* 05 */
			new OpCode( null                ,  0 ,  0 ), /* 06 */
			new OpCode( null                ,  0 ,  0 ), /* 07 */
			new OpCode( null                ,  0 ,  0 ), /* 08 */
			new OpCode( "ADD IY,BC"         , 15 ,  0 ), /* 09 */
			new OpCode( null                ,  0 ,  0 ), /* 0A */
			new OpCode( null                ,  0 ,  0 ), /* 0B */
			new OpCode( null                ,  0 ,  0 ), /* 0C */
			new OpCode( null                ,  0 ,  0 ), /* 0D */
			new OpCode( null                ,  0 ,  0 ), /* 0E */
			new OpCode( null                ,  0 ,  0 ), /* 0F */
			new OpCode( null                ,  0 ,  0 ), /* 10 */
			new OpCode( null                ,  0 ,  0 ), /* 11 */
			new OpCode( null                ,  0 ,  0 ), /* 12 */
			new OpCode( null                ,  0 ,  0 ), /* 13 */
			new OpCode( null                ,  0 ,  0 ), /* 14 */
			new OpCode( null                ,  0 ,  0 ), /* 15 */
			new OpCode( null                ,  0 ,  0 ), /* 16 */
			new OpCode( null                ,  0 ,  0 ), /* 17 */
			new OpCode( null                ,  0 ,  0 ), /* 18 */
			new OpCode( "ADD IY,DE"         , 15 ,  0 ), /* 19 */
			new OpCode( null                ,  0 ,  0 ), /* 1A */
			new OpCode( null                ,  0 ,  0 ), /* 1B */
			new OpCode( null                ,  0 ,  0 ), /* 1C */
			new OpCode( null                ,  0 ,  0 ), /* 1D */
			new OpCode( null                ,  0 ,  0 ), /* 1E */
			new OpCode( null                ,  0 ,  0 ), /* 1F */
			new OpCode( null                ,  0 ,  0 ), /* 20 */
			new OpCode( "LD IY,@"           , 14 ,  0 ), /* 21 */
			new OpCode( "LD (@),IY"         , 20 ,  0 ), /* 22 */
			new OpCode( "INC IY"            , 10 ,  0 ), /* 23 */
			new OpCode( "INC IYH"           ,  8 ,  0 ), /* 24 */
			new OpCode( "DEC IYH"           ,  8 ,  0 ), /* 25 */
			new OpCode( "LD IYH,#"          , 11 ,  0 ), /* 26 */
			new OpCode( null                ,  0 ,  0 ), /* 27 */
			new OpCode( null                ,  0 ,  0 ), /* 28 */
			new OpCode( "ADD IY,IY"         , 15 ,  0 ), /* 29 */
			new OpCode( "LD IY,(@)"         , 20 ,  0 ), /* 2A */
			new OpCode( "DEC IY"            , 10 ,  0 ), /* 2B */
			new OpCode( "INC IYL"           ,  8 ,  0 ), /* 2C */
			new OpCode( "DEC IYL"           ,  8 ,  0 ), /* 2D */
			new OpCode( "LD IYL,#"          , 11 ,  0 ), /* 2E */
			new OpCode( null                ,  0 ,  0 ), /* 2F */
			new OpCode( null                ,  0 ,  0 ), /* 30 */
			new OpCode( null                ,  0 ,  0 ), /* 31 */
			new OpCode( null                ,  0 ,  0 ), /* 32 */
			new OpCode( null                ,  0 ,  0 ), /* 33 */
			new OpCode( "INC (IY+$)"        , 23 ,  0 ), /* 34 */
			new OpCode( "DEC (IY+$)"        , 23 ,  0 ), /* 35 */
			new OpCode( "LD (IY+$),#"       , 19 ,  0 ), /* 36 */
			new OpCode( null                ,  0 ,  0 ), /* 37 */
			new OpCode( null                ,  0 ,  0 ), /* 38 */
			new OpCode( "ADD IY,SP"         , 15 ,  0 ), /* 39 */
			new OpCode( null                ,  0 ,  0 ), /* 3A */
			new OpCode( null                ,  0 ,  0 ), /* 3B */
			new OpCode( null                ,  0 ,  0 ), /* 3C */
			new OpCode( null                ,  0 ,  0 ), /* 3D */
			new OpCode( null                ,  0 ,  0 ), /* 3E */
			new OpCode( null                ,  0 ,  0 ), /* 3F */
			new OpCode( null                ,  0 ,  0 ), /* 40 */
			new OpCode( null                ,  0 ,  0 ), /* 41 */
			new OpCode( null                ,  0 ,  0 ), /* 42 */
			new OpCode( null                ,  0 ,  0 ), /* 43 */
			new OpCode( "LD B,IYH"          ,  8 ,  0 ), /* 44 */
			new OpCode( "LD B,IYL"          ,  8 ,  0 ), /* 45 */
			new OpCode( "LD B,(IY+$)"       , 19 ,  0 ), /* 46 */
			new OpCode( null                ,  0 ,  0 ), /* 47 */
			new OpCode( null                ,  0 ,  0 ), /* 48 */
			new OpCode( null                ,  0 ,  0 ), /* 49 */
			new OpCode( null                ,  0 ,  0 ), /* 4A */
			new OpCode( null                ,  0 ,  0 ), /* 4B */
			new OpCode( "LD C,IYH"          ,  8 ,  0 ), /* 4C */
			new OpCode( "LD C,IYL"          ,  8 ,  0 ), /* 4D */
			new OpCode( "LD C,(IY+$)"       , 19 ,  0 ), /* 4E */
			new OpCode( null                ,  0 ,  0 ), /* 4F */
			new OpCode( null                ,  0 ,  0 ), /* 50 */
			new OpCode( null                ,  0 ,  0 ), /* 51 */
			new OpCode( null                ,  0 ,  0 ), /* 52 */
			new OpCode( null                ,  0 ,  0 ), /* 53 */
			new OpCode( "LD D,IYH"          ,  8 ,  0 ), /* 54 */
			new OpCode( "LD D,IYL"          ,  8 ,  0 ), /* 55 */
			new OpCode( "LD D,(IY+$)"       , 19 ,  0 ), /* 56 */
			new OpCode( null                ,  0 ,  0 ), /* 57 */
			new OpCode( null                ,  0 ,  0 ), /* 58 */
			new OpCode( null                ,  0 ,  0 ), /* 59 */
			new OpCode( null                ,  0 ,  0 ), /* 5A */
			new OpCode( null                ,  0 ,  0 ), /* 5B */
			new OpCode( "LD E,IYH"          ,  8 ,  0 ), /* 5C */
			new OpCode( "LD E,IYL"          ,  8 ,  0 ), /* 5D */
			new OpCode( "LD E,(IY+$)"       , 19 ,  0 ), /* 5E */
			new OpCode( null                ,  0 ,  0 ), /* 5F */
			new OpCode( "LD IYH,B"          ,  8 ,  0 ), /* 60 */
			new OpCode( "LD IYH,C"          ,  8 ,  0 ), /* 61 */
			new OpCode( "LD IYH,D"          ,  8 ,  0 ), /* 62 */
			new OpCode( "LD IYH,E"          ,  8 ,  0 ), /* 63 */
			new OpCode( "LD IYH,IYH"        ,  8 ,  0 ), /* 64 */
			new OpCode( "LD IYH,IYL"        ,  8 ,  0 ), /* 65 */
			new OpCode( "LD H,(IY+$)"       , 19 ,  0 ), /* 66 */
			new OpCode( "LD IYH,A"          ,  8 ,  0 ), /* 67 */
			new OpCode( "LD IYL,B"          ,  8 ,  0 ), /* 68 */
			new OpCode( "LD IYL,C"          ,  8 ,  0 ), /* 69 */
			new OpCode( "LD IYL,D"          ,  8 ,  0 ), /* 6A */
			new OpCode( "LD IYL,E"          ,  8 ,  0 ), /* 6B */
			new OpCode( "LD IYL,IYH"        ,  8 ,  0 ), /* 6C */
			new OpCode( "LD IYL,IYL"        ,  8 ,  0 ), /* 6D */
			new OpCode( "LD L,(IY+$)"       , 19 ,  0 ), /* 6E */
			new OpCode( "LD IYL,A"          ,  8 ,  0 ), /* 6F */
			new OpCode( "LD (IY+$),B"       , 19 ,  0 ), /* 70 */
			new OpCode( "LD (IY+$),C"       , 19 ,  0 ), /* 71 */
			new OpCode( "LD (IY+$),D"       , 19 ,  0 ), /* 72 */
			new OpCode( "LD (IY+$),E"       , 19 ,  0 ), /* 73 */
			new OpCode( "LD (IY+$),H"       , 19 ,  0 ), /* 74 */
			new OpCode( "LD (IY+$),L"       , 19 ,  0 ), /* 75 */
			new OpCode( null                ,  0 ,  0 ), /* 76 */
			new OpCode( "LD (IY+$),A"       , 19 ,  0 ), /* 77 */
			new OpCode( null                ,  0 ,  0 ), /* 78 */
			new OpCode( null                ,  0 ,  0 ), /* 79 */
			new OpCode( null                ,  0 ,  0 ), /* 7A */
			new OpCode( null                ,  0 ,  0 ), /* 7B */
			new OpCode( "LD A,IYH"          ,  8 ,  0 ), /* 7C */
			new OpCode( "LD A,IYL"          ,  8 ,  0 ), /* 7D */
			new OpCode( "LD A,(IY+$)"       , 19 ,  0 ), /* 7E */
			new OpCode( null                ,  0 ,  0 ), /* 7F */
			new OpCode( null                ,  0 ,  0 ), /* 80 */
			new OpCode( null                ,  0 ,  0 ), /* 81 */
			new OpCode( null                ,  0 ,  0 ), /* 82 */
			new OpCode( null                ,  0 ,  0 ), /* 83 */
			new OpCode( "ADD A,IYH"         ,  8 ,  0 ), /* 84 */
			new OpCode( "ADD A,IYL"         ,  8 ,  0 ), /* 85 */
			new OpCode( "ADD A,(IY+$)"      , 19 ,  0 ), /* 86 */
			new OpCode( null                ,  0 ,  0 ), /* 87 */
			new OpCode( null                ,  0 ,  0 ), /* 88 */
			new OpCode( null                ,  0 ,  0 ), /* 89 */
			new OpCode( null                ,  0 ,  0 ), /* 8A */
			new OpCode( null                ,  0 ,  0 ), /* 8B */
			new OpCode( "ADC A,IYH"         ,  8 ,  0 ), /* 8C */
			new OpCode( "ADC A,IYL"         ,  8 ,  0 ), /* 8D */
			new OpCode( "ADC A,(IY+$)"      , 19 ,  0 ), /* 8E */
			new OpCode( null                ,  0 ,  0 ), /* 8F */
			new OpCode( null                ,  0 ,  0 ), /* 90 */
			new OpCode( null                ,  0 ,  0 ), /* 91 */
			new OpCode( null                ,  0 ,  0 ), /* 92 */
			new OpCode( null                ,  0 ,  0 ), /* 93 */
			new OpCode( "SUB IYH"           ,  8 ,  0 ), /* 94 */
			new OpCode( "SUB IYL"           ,  8 ,  0 ), /* 95 */
			new OpCode( "SUB (IY+$)"        , 19 ,  0 ), /* 96 */
			new OpCode( null                ,  0 ,  0 ), /* 97 */
			new OpCode( null                ,  0 ,  0 ), /* 98 */
			new OpCode( null                ,  0 ,  0 ), /* 99 */
			new OpCode( null                ,  0 ,  0 ), /* 9A */
			new OpCode( null                ,  0 ,  0 ), /* 9B */
			new OpCode( "SBC A,IYH"         ,  8 ,  0 ), /* 9C */
			new OpCode( "SBC A,IYL"         ,  8 ,  0 ), /* 9D */
			new OpCode( "SBC A,(IY+$)"      , 19 ,  0 ), /* 9E */
			new OpCode( null                ,  0 ,  0 ), /* 9F */
			new OpCode( null                ,  0 ,  0 ), /* A0 */
			new OpCode( null                ,  0 ,  0 ), /* A1 */
			new OpCode( null                ,  0 ,  0 ), /* A2 */
			new OpCode( null                ,  0 ,  0 ), /* A3 */
			new OpCode( "AND IYH"           ,  8 ,  0 ), /* A4 */
			new OpCode( "AND IYL"           ,  8 ,  0 ), /* A5 */
			new OpCode( "AND (IY+$)"        , 19 ,  0 ), /* A6 */
			new OpCode( null                ,  0 ,  0 ), /* A7 */
			new OpCode( null                ,  0 ,  0 ), /* A8 */
			new OpCode( null                ,  0 ,  0 ), /* A9 */
			new OpCode( null                ,  0 ,  0 ), /* AA */
			new OpCode( null                ,  0 ,  0 ), /* AB */
			new OpCode( "XOR IYH"           ,  8 ,  0 ), /* AC */
			new OpCode( "XOR IYL"           ,  8 ,  0 ), /* AD */
			new OpCode( "XOR (IY+$)"        , 19 ,  0 ), /* AE */
			new OpCode( null                ,  0 ,  0 ), /* AF */
			new OpCode( null                ,  0 ,  0 ), /* B0 */
			new OpCode( null                ,  0 ,  0 ), /* B1 */
			new OpCode( null                ,  0 ,  0 ), /* B2 */
			new OpCode( null                ,  0 ,  0 ), /* B3 */
			new OpCode( "OR IYH"            ,  8 ,  0 ), /* B4 */
			new OpCode( "OR IYL"            ,  8 ,  0 ), /* B5 */
			new OpCode( "OR (IY+$)"         , 19 ,  0 ), /* B6 */
			new OpCode( null                ,  0 ,  0 ), /* B7 */
			new OpCode( null                ,  0 ,  0 ), /* B8 */
			new OpCode( null                ,  0 ,  0 ), /* B9 */
			new OpCode( null                ,  0 ,  0 ), /* BA */
			new OpCode( null                ,  0 ,  0 ), /* BB */
			new OpCode( "CP IYH"            ,  8 ,  0 ), /* BC */
			new OpCode( "CP IYL"            ,  8 ,  0 ), /* BD */
			new OpCode( "CP (IY+$)"         , 19 ,  0 ), /* BE */
			new OpCode( null                ,  0 ,  0 ), /* BF */
			new OpCode( null                ,  0 ,  0 ), /* C0 */
			new OpCode( null                ,  0 ,  0 ), /* C1 */
			new OpCode( null                ,  0 ,  0 ), /* C2 */
			new OpCode( null                ,  0 ,  0 ), /* C3 */
			new OpCode( null                ,  0 ,  0 ), /* C4 */
			new OpCode( null                ,  0 ,  0 ), /* C5 */
			new OpCode( null                ,  0 ,  0 ), /* C6 */
			new OpCode( null                ,  0 ,  0 ), /* C7 */
			new OpCode( null                ,  0 ,  0 ), /* C8 */
			new OpCode( null                ,  0 ,  0 ), /* C9 */
			new OpCode( null                ,  0 ,  0 ), /* CA */
			new OpCode( "shift CB"          ,  0 ,  0 ), /* CB */
			new OpCode( null                ,  0 ,  0 ), /* CC */
			new OpCode( null                ,  0 ,  0 ), /* CD */
			new OpCode( null                ,  0 ,  0 ), /* CE */
			new OpCode( null                ,  0 ,  0 ), /* CF */
			new OpCode( null                ,  0 ,  0 ), /* D0 */
			new OpCode( null                ,  0 ,  0 ), /* D1 */
			new OpCode( null                ,  0 ,  0 ), /* D2 */
			new OpCode( null                ,  0 ,  0 ), /* D3 */
			new OpCode( null                ,  0 ,  0 ), /* D4 */
			new OpCode( null                ,  0 ,  0 ), /* D5 */
			new OpCode( null                ,  0 ,  0 ), /* D6 */
			new OpCode( null                ,  0 ,  0 ), /* D7 */
			new OpCode( null                ,  0 ,  0 ), /* D8 */
			new OpCode( null                ,  0 ,  0 ), /* D9 */
			new OpCode( null                ,  0 ,  0 ), /* DA */
			new OpCode( null                ,  0 ,  0 ), /* DB */
			new OpCode( null                ,  0 ,  0 ), /* DC */
			new OpCode( "ignore"            ,  4 ,  0 ), /* DD */
			new OpCode( null                ,  0 ,  0 ), /* DE */
			new OpCode( null                ,  0 ,  0 ), /* DF */
			new OpCode( null                ,  0 ,  0 ), /* E0 */
			new OpCode( "POP IY"            , 14 ,  0 ), /* E1 */
			new OpCode( null                ,  0 ,  0 ), /* E2 */
			new OpCode( "EX (SP),IY"        , 23 ,  0 ), /* E3 */
			new OpCode( null                ,  0 ,  0 ), /* E4 */
			new OpCode( "PUSH IY"           , 15 ,  0 ), /* E5 */
			new OpCode( null                ,  0 ,  0 ), /* E6 */
			new OpCode( null                ,  0 ,  0 ), /* E7 */
			new OpCode( null                ,  0 ,  0 ), /* E8 */
			new OpCode( "JP (IY)"             ,  8 ,  0 , OpCodeFlags.Jumps ), /* E9 */
			new OpCode( null                ,  0 ,  0 ), /* EA */
			new OpCode( null                ,  0 ,  0 ), /* EB */
			new OpCode( null                ,  0 ,  0 ), /* EC */
			new OpCode( null                ,  4 ,  0 ), /* ED */
			new OpCode( null                ,  0 ,  0 ), /* EE */
			new OpCode( null                ,  0 ,  0 ), /* EF */
			new OpCode( null                ,  0 ,  0 ), /* F0 */
			new OpCode( null                ,  0 ,  0 ), /* F1 */
			new OpCode( null                ,  0 ,  0 ), /* F2 */
			new OpCode( null                ,  0 ,  0 ), /* F3 */
			new OpCode( null                ,  0 ,  0 ), /* F4 */
			new OpCode( null                ,  0 ,  0 ), /* F5 */
			new OpCode( null                ,  0 ,  0 ), /* F6 */
			new OpCode( null                ,  0 ,  0 ), /* F7 */
			new OpCode( null                ,  0 ,  0 ), /* F8 */
			new OpCode( "LD SP,IY"          , 10 ,  0 ), /* F9 */
			new OpCode( null                ,  0 ,  0 ), /* FA */
			new OpCode( null                ,  0 ,  0 ), /* FB */
			new OpCode( null                ,  0 ,  0 ), /* FC */
			new OpCode( "ignore"            ,  4 ,  0 ), /* FD */
			new OpCode( null                ,  0 ,  0 ), /* FE */
			new OpCode( null                ,  0 ,  0 ), /* FF */
		};


		/**/
		public static OpCode[] dasm_ddcb = new OpCode[] 
		{
			new OpCode( "LD B,RLC (IX+$)"   , 23 ,  0 ), /* 00 */
			new OpCode( "LD C,RLC (IX+$)"   , 23 ,  0 ), /* 01 */
			new OpCode( "LD D,RLC (IX+$)"   , 23 ,  0 ), /* 02 */
			new OpCode( "LD E,RLC (IX+$)"   , 23 ,  0 ), /* 03 */
			new OpCode( "LD H,RLC (IX+$)"   , 23 ,  0 ), /* 04 */
			new OpCode( "LD L,RLC (IX+$)"   , 23 ,  0 ), /* 05 */
			new OpCode( "RLC (IX+$)"        , 23 ,  0 ), /* 06 */
			new OpCode( "LD A,RLC (IX+$)"   , 23 ,  0 ), /* 07 */
			new OpCode( "LD B,RRC (IX+$)"   , 23 ,  0 ), /* 08 */
			new OpCode( "LD C,RRC (IX+$)"   , 23 ,  0 ), /* 09 */
			new OpCode( "LD D,RRC (IX+$)"   , 23 ,  0 ), /* 0A */
			new OpCode( "LD E,RRC (IX+$)"   , 23 ,  0 ), /* 0B */
			new OpCode( "LD H,RRC (IX+$)"   , 23 ,  0 ), /* 0C */
			new OpCode( "LD L,RRC (IX+$)"   , 23 ,  0 ), /* 0D */
			new OpCode( "RRC (IX+$)"        , 23 ,  0 ), /* 0E */
			new OpCode( "LD A,RRC (IX+$)"   , 23 ,  0 ), /* 0F */
			new OpCode( "LD B,RL (IX+$)"    , 23 ,  0 ), /* 10 */
			new OpCode( "LD C,RL (IX+$)"    , 23 ,  0 ), /* 11 */
			new OpCode( "LD D,RL (IX+$)"    , 23 ,  0 ), /* 12 */
			new OpCode( "LD E,RL (IX+$)"    , 23 ,  0 ), /* 13 */
			new OpCode( "LD H,RL (IX+$)"    , 23 ,  0 ), /* 14 */
			new OpCode( "LD L,RL (IX+$)"    , 23 ,  0 ), /* 15 */
			new OpCode( "RL (IX+$)"         , 23 ,  0 ), /* 16 */
			new OpCode( "LD A,RL (IX+$)"    , 23 ,  0 ), /* 17 */
			new OpCode( "LD B,RR (IX+$)"    , 23 ,  0 ), /* 18 */
			new OpCode( "LD C,RR (IX+$)"    , 23 ,  0 ), /* 19 */
			new OpCode( "LD D,RR (IX+$)"    , 23 ,  0 ), /* 1A */
			new OpCode( "LD E,RR (IX+$)"    , 23 ,  0 ), /* 1B */
			new OpCode( "LD H,RR (IX+$)"    , 23 ,  0 ), /* 1C */
			new OpCode( "LD L,RR (IX+$)"    , 23 ,  0 ), /* 1D */
			new OpCode( "RR (IX+$)"         , 23 ,  0 ), /* 1E */
			new OpCode( "LD A,RR (IX+$)"    , 23 ,  0 ), /* 1F */
			new OpCode( "LD B,SLA (IX+$)"   , 23 ,  0 ), /* 20 */
			new OpCode( "LD C,SLA (IX+$)"   , 23 ,  0 ), /* 21 */
			new OpCode( "LD D,SLA (IX+$)"   , 23 ,  0 ), /* 22 */
			new OpCode( "LD E,SLA (IX+$)"   , 23 ,  0 ), /* 23 */
			new OpCode( "LD H,SLA (IX+$)"   , 23 ,  0 ), /* 24 */
			new OpCode( "LD L,SLA (IX+$)"   , 23 ,  0 ), /* 25 */
			new OpCode( "SLA (IX+$)"        , 23 ,  0 ), /* 26 */
			new OpCode( "LD A,SLA (IX+$)"   , 23 ,  0 ), /* 27 */
			new OpCode( "LD B,SRA (IX+$)"   , 23 ,  0 ), /* 28 */
			new OpCode( "LD C,SRA (IX+$)"   , 23 ,  0 ), /* 29 */
			new OpCode( "LD D,SRA (IX+$)"   , 23 ,  0 ), /* 2A */
			new OpCode( "LD E,SRA (IX+$)"   , 23 ,  0 ), /* 2B */
			new OpCode( "LD H,SRA (IX+$)"   , 23 ,  0 ), /* 2C */
			new OpCode( "LD L,SRA (IX+$)"   , 23 ,  0 ), /* 2D */
			new OpCode( "SRA (IX+$)"        , 23 ,  0 ), /* 2E */
			new OpCode( "LD A,SRA (IX+$)"   , 23 ,  0 ), /* 2F */
			new OpCode( "LD B,SLL (IX+$)"   , 23 ,  0 ), /* 30 */
			new OpCode( "LD C,SLL (IX+$)"   , 23 ,  0 ), /* 31 */
			new OpCode( "LD D,SLL (IX+$)"   , 23 ,  0 ), /* 32 */
			new OpCode( "LD E,SLL (IX+$)"   , 23 ,  0 ), /* 33 */
			new OpCode( "LD H,SLL (IX+$)"   , 23 ,  0 ), /* 34 */
			new OpCode( "LD L,SLL (IX+$)"   , 23 ,  0 ), /* 35 */
			new OpCode( "SLL (IX+$)"        , 23 ,  0 ), /* 36 */
			new OpCode( "LD A,SLL (IX+$)"   , 23 ,  0 ), /* 37 */
			new OpCode( "LD B,SRL (IX+$)"   , 23 ,  0 ), /* 38 */
			new OpCode( "LD C,SRL (IX+$)"   , 23 ,  0 ), /* 39 */
			new OpCode( "LD D,SRL (IX+$)"   , 23 ,  0 ), /* 3A */
			new OpCode( "LD E,SRL (IX+$)"   , 23 ,  0 ), /* 3B */
			new OpCode( "LD H,SRL (IX+$)"   , 23 ,  0 ), /* 3C */
			new OpCode( "LD L,SRL (IX+$)"   , 23 ,  0 ), /* 3D */
			new OpCode( "SRL (IX+$)"        , 23 ,  0 ), /* 3E */
			new OpCode( "LD A,SRL (IX+$)"   , 23 ,  0 ), /* 3F */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 40 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 41 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 42 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 43 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 44 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 45 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 46 */
			new OpCode( "BIT 0,(IX+$)"      , 20 ,  0 ), /* 47 */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 48 */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 49 */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 4A */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 4B */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 4C */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 4D */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 4E */
			new OpCode( "BIT 1,(IX+$)"      , 20 ,  0 ), /* 4F */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 50 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 51 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 52 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 53 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 54 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 55 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 56 */
			new OpCode( "BIT 2,(IX+$)"      , 20 ,  0 ), /* 57 */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 58 */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 59 */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 5A */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 5B */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 5C */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 5D */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 5E */
			new OpCode( "BIT 3,(IX+$)"      , 20 ,  0 ), /* 5F */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 60 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 61 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 62 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 63 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 64 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 65 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 66 */
			new OpCode( "BIT 4,(IX+$)"      , 20 ,  0 ), /* 67 */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 68 */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 69 */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 6A */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 6B */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 6C */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 6D */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 6E */
			new OpCode( "BIT 5,(IX+$)"      , 20 ,  0 ), /* 6F */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 70 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 71 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 72 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 73 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 74 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 75 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 76 */
			new OpCode( "BIT 6,(IX+$)"      , 20 ,  0 ), /* 77 */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 78 */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 79 */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 7A */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 7B */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 7C */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 7D */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 7E */
			new OpCode( "BIT 7,(IX+$)"      , 20 ,  0 ), /* 7F */
			new OpCode( "LD B,RES 0,(IX+$)" , 23 ,  0 ), /* 80 */
			new OpCode( "LD C,RES 0,(IX+$)" , 23 ,  0 ), /* 81 */
			new OpCode( "LD D,RES 0,(IX+$)" , 23 ,  0 ), /* 82 */
			new OpCode( "LD E,RES 0,(IX+$)" , 23 ,  0 ), /* 83 */
			new OpCode( "LD H,RES 0,(IX+$)" , 23 ,  0 ), /* 84 */
			new OpCode( "LD L,RES 0,(IX+$)" , 23 ,  0 ), /* 85 */
			new OpCode( "RES 0,(IX+$)"      , 23 ,  0 ), /* 86 */
			new OpCode( "LD A,RES 0,(IX+$)" , 23 ,  0 ), /* 87 */
			new OpCode( "LD B,RES 1,(IX+$)" , 23 ,  0 ), /* 88 */
			new OpCode( "LD C,RES 1,(IX+$)" , 23 ,  0 ), /* 89 */
			new OpCode( "LD D,RES 1,(IX+$)" , 23 ,  0 ), /* 8A */
			new OpCode( "LD E,RES 1,(IX+$)" , 23 ,  0 ), /* 8B */
			new OpCode( "LD H,RES 1,(IX+$)" , 23 ,  0 ), /* 8C */
			new OpCode( "LD L,RES 1,(IX+$)" , 23 ,  0 ), /* 8D */
			new OpCode( "RES 1,(IX+$)"      , 23 ,  0 ), /* 8E */
			new OpCode( "LD A,RES 1,(IX+$)" , 23 ,  0 ), /* 8F */
			new OpCode( "LD B,RES 2,(IX+$)" , 23 ,  0 ), /* 90 */
			new OpCode( "LD C,RES 2,(IX+$)" , 23 ,  0 ), /* 91 */
			new OpCode( "LD D,RES 2,(IX+$)" , 23 ,  0 ), /* 92 */
			new OpCode( "LD E,RES 2,(IX+$)" , 23 ,  0 ), /* 93 */
			new OpCode( "LD H,RES 2,(IX+$)" , 23 ,  0 ), /* 94 */
			new OpCode( "LD L,RES 2,(IX+$)" , 23 ,  0 ), /* 95 */
			new OpCode( "RES 2,(IX+$)"      , 23 ,  0 ), /* 96 */
			new OpCode( "LD A,RES 2,(IX+$)" , 23 ,  0 ), /* 97 */
			new OpCode( "LD B,RES 3,(IX+$)" , 23 ,  0 ), /* 98 */
			new OpCode( "LD C,RES 3,(IX+$)" , 23 ,  0 ), /* 99 */
			new OpCode( "LD D,RES 3,(IX+$)" , 23 ,  0 ), /* 9A */
			new OpCode( "LD E,RES 3,(IX+$)" , 23 ,  0 ), /* 9B */
			new OpCode( "LD H,RES 3,(IX+$)" , 23 ,  0 ), /* 9C */
			new OpCode( "LD L,RES 3,(IX+$)" , 23 ,  0 ), /* 9D */
			new OpCode( "RES 3,(IX+$)"      , 23 ,  0 ), /* 9E */
			new OpCode( "LD A,RES 3,(IX+$)" , 23 ,  0 ), /* 9F */
			new OpCode( "LD B,RES 4,(IX+$)" , 23 ,  0 ), /* A0 */
			new OpCode( "LD C,RES 4,(IX+$)" , 23 ,  0 ), /* A1 */
			new OpCode( "LD D,RES 4,(IX+$)" , 23 ,  0 ), /* A2 */
			new OpCode( "LD E,RES 4,(IX+$)" , 23 ,  0 ), /* A3 */
			new OpCode( "LD H,RES 4,(IX+$)" , 23 ,  0 ), /* A4 */
			new OpCode( "LD L,RES 4,(IX+$)" , 23 ,  0 ), /* A5 */
			new OpCode( "RES 4,(IX+$)"      , 23 ,  0 ), /* A6 */
			new OpCode( "LD A,RES 4,(IX+$)" , 23 ,  0 ), /* A7 */
			new OpCode( "LD B,RES 5,(IX+$)" , 23 ,  0 ), /* A8 */
			new OpCode( "LD C,RES 5,(IX+$)" , 23 ,  0 ), /* A9 */
			new OpCode( "LD D,RES 5,(IX+$)" , 23 ,  0 ), /* AA */
			new OpCode( "LD E,RES 5,(IX+$)" , 23 ,  0 ), /* AB */
			new OpCode( "LD H,RES 5,(IX+$)" , 23 ,  0 ), /* AC */
			new OpCode( "LD L,RES 5,(IX+$)" , 23 ,  0 ), /* AD */
			new OpCode( "RES 5,(IX+$)"      , 23 ,  0 ), /* AE */
			new OpCode( "LD A,RES 5,(IX+$)" , 23 ,  0 ), /* AF */
			new OpCode( "LD B,RES 6,(IX+$)" , 23 ,  0 ), /* B0 */
			new OpCode( "LD C,RES 6,(IX+$)" , 23 ,  0 ), /* B1 */
			new OpCode( "LD D,RES 6,(IX+$)" , 23 ,  0 ), /* B2 */
			new OpCode( "LD E,RES 6,(IX+$)" , 23 ,  0 ), /* B3 */
			new OpCode( "LD H,RES 6,(IX+$)" , 23 ,  0 ), /* B4 */
			new OpCode( "LD L,RES 6,(IX+$)" , 23 ,  0 ), /* B5 */
			new OpCode( "RES 6,(IX+$)"      , 23 ,  0 ), /* B6 */
			new OpCode( "LD A,RES 6,(IX+$)" , 23 ,  0 ), /* B7 */
			new OpCode( "LD B,RES 7,(IX+$)" , 23 ,  0 ), /* B8 */
			new OpCode( "LD C,RES 7,(IX+$)" , 23 ,  0 ), /* B9 */
			new OpCode( "LD D,RES 7,(IX+$)" , 23 ,  0 ), /* BA */
			new OpCode( "LD E,RES 7,(IX+$)" , 23 ,  0 ), /* BB */
			new OpCode( "LD H,RES 7,(IX+$)" , 23 ,  0 ), /* BC */
			new OpCode( "LD L,RES 7,(IX+$)" , 23 ,  0 ), /* BD */
			new OpCode( "RES 7,(IX+$)"      , 23 ,  0 ), /* BE */
			new OpCode( "LD A,RES 7,(IX+$)" , 23 ,  0 ), /* BF */
			new OpCode( "LD B,SET 0,(IX+$)" , 23 ,  0 ), /* C0 */
			new OpCode( "LD C,SET 0,(IX+$)" , 23 ,  0 ), /* C1 */
			new OpCode( "LD D,SET 0,(IX+$)" , 23 ,  0 ), /* C2 */
			new OpCode( "LD E,SET 0,(IX+$)" , 23 ,  0 ), /* C3 */
			new OpCode( "LD H,SET 0,(IX+$)" , 23 ,  0 ), /* C4 */
			new OpCode( "LD L,SET 0,(IX+$)" , 23 ,  0 ), /* C5 */
			new OpCode( "SET 0,(IX+$)"      , 23 ,  0 ), /* C6 */
			new OpCode( "LD A,SET 0,(IX+$)" , 23 ,  0 ), /* C7 */
			new OpCode( "LD B,SET 1,(IX+$)" , 23 ,  0 ), /* C8 */
			new OpCode( "LD C,SET 1,(IX+$)" , 23 ,  0 ), /* C9 */
			new OpCode( "LD D,SET 1,(IX+$)" , 23 ,  0 ), /* CA */
			new OpCode( "LD E,SET 1,(IX+$)" , 23 ,  0 ), /* CB */
			new OpCode( "LD H,SET 1,(IX+$)" , 23 ,  0 ), /* CC */
			new OpCode( "LD L,SET 1,(IX+$)" , 23 ,  0 ), /* CD */
			new OpCode( "SET 1,(IX+$)"      , 23 ,  0 ), /* CE */
			new OpCode( "LD A,SET 1,(IX+$)" , 23 ,  0 ), /* CF */
			new OpCode( "LD B,SET 2,(IX+$)" , 23 ,  0 ), /* D0 */
			new OpCode( "LD C,SET 2,(IX+$)" , 23 ,  0 ), /* D1 */
			new OpCode( "LD D,SET 2,(IX+$)" , 23 ,  0 ), /* D2 */
			new OpCode( "LD E,SET 2,(IX+$)" , 23 ,  0 ), /* D3 */
			new OpCode( "LD H,SET 2,(IX+$)" , 23 ,  0 ), /* D4 */
			new OpCode( "LD L,SET 2,(IX+$)" , 23 ,  0 ), /* D5 */
			new OpCode( "SET 2,(IX+$)"      , 23 ,  0 ), /* D6 */
			new OpCode( "LD A,SET 2,(IX+$)" , 23 ,  0 ), /* D7 */
			new OpCode( "LD B,SET 3,(IX+$)" , 23 ,  0 ), /* D8 */
			new OpCode( "LD C,SET 3,(IX+$)" , 23 ,  0 ), /* D9 */
			new OpCode( "LD D,SET 3,(IX+$)" , 23 ,  0 ), /* DA */
			new OpCode( "LD E,SET 3,(IX+$)" , 23 ,  0 ), /* DB */
			new OpCode( "LD H,SET 3,(IX+$)" , 23 ,  0 ), /* DC */
			new OpCode( "LD L,SET 3,(IX+$)" , 23 ,  0 ), /* DD */
			new OpCode( "SET 3,(IX+$)"      , 23 ,  0 ), /* DE */
			new OpCode( "LD A,SET 3,(IX+$)" , 23 ,  0 ), /* DF */
			new OpCode( "LD B,SET 4,(IX+$)" , 23 ,  0 ), /* E0 */
			new OpCode( "LD C,SET 4,(IX+$)" , 23 ,  0 ), /* E1 */
			new OpCode( "LD D,SET 4,(IX+$)" , 23 ,  0 ), /* E2 */
			new OpCode( "LD E,SET 4,(IX+$)" , 23 ,  0 ), /* E3 */
			new OpCode( "LD H,SET 4,(IX+$)" , 23 ,  0 ), /* E4 */
			new OpCode( "LD L,SET 4,(IX+$)" , 23 ,  0 ), /* E5 */
			new OpCode( "SET 4,(IX+$)"      , 23 ,  0 ), /* E6 */
			new OpCode( "LD A,SET 4,(IX+$)" , 23 ,  0 ), /* E7 */
			new OpCode( "LD B,SET 5,(IX+$)" , 23 ,  0 ), /* E8 */
			new OpCode( "LD C,SET 5,(IX+$)" , 23 ,  0 ), /* E9 */
			new OpCode( "LD D,SET 5,(IX+$)" , 23 ,  0 ), /* EA */
			new OpCode( "LD E,SET 5,(IX+$)" , 23 ,  0 ), /* EB */
			new OpCode( "LD H,SET 5,(IX+$)" , 23 ,  0 ), /* EC */
			new OpCode( "LD L,SET 5,(IX+$)" , 23 ,  0 ), /* ED */
			new OpCode( "SET 5,(IX+$)"      , 23 ,  0 ), /* EE */
			new OpCode( "LD A,SET 5,(IX+$)" , 23 ,  0 ), /* EF */
			new OpCode( "LD B,SET 6,(IX+$)" , 23 ,  0 ), /* F0 */
			new OpCode( "LD C,SET 6,(IX+$)" , 23 ,  0 ), /* F1 */
			new OpCode( "LD D,SET 6,(IX+$)" , 23 ,  0 ), /* F2 */
			new OpCode( "LD E,SET 6,(IX+$)" , 23 ,  0 ), /* F3 */
			new OpCode( "LD H,SET 6,(IX+$)" , 23 ,  0 ), /* F4 */
			new OpCode( "LD L,SET 6,(IX+$)" , 23 ,  0 ), /* F5 */
			new OpCode( "SET 6,(IX+$)"      , 23 ,  0 ), /* F6 */
			new OpCode( "LD A,SET 6,(IX+$)" , 23 ,  0 ), /* F7 */
			new OpCode( "LD B,SET 7,(IX+$)" , 23 ,  0 ), /* F8 */
			new OpCode( "LD C,SET 7,(IX+$)" , 23 ,  0 ), /* F9 */
			new OpCode( "LD D,SET 7,(IX+$)" , 23 ,  0 ), /* FA */
			new OpCode( "LD E,SET 7,(IX+$)" , 23 ,  0 ), /* FB */
			new OpCode( "LD H,SET 7,(IX+$)" , 23 ,  0 ), /* FC */
			new OpCode( "LD L,SET 7,(IX+$)" , 23 ,  0 ), /* FD */
			new OpCode( "SET 7,(IX+$)"      , 23 ,  0 ), /* FE */
			new OpCode( "LD A,SET 7,(IX+$)" , 23 ,  0 ), /* FF */
		};


		/**/
		public static OpCode[] dasm_fdcb = new OpCode[]
		{
			new OpCode( "LD B,RLC (IY+$)"   , 23 ,  0 ), /* 00 */
			new OpCode( "LD C,RLC (IY+$)"   , 23 ,  0 ), /* 01 */
			new OpCode( "LD D,RLC (IY+$)"   , 23 ,  0 ), /* 02 */
			new OpCode( "LD E,RLC (IY+$)"   , 23 ,  0 ), /* 03 */
			new OpCode( "LD H,RLC (IY+$)"   , 23 ,  0 ), /* 04 */
			new OpCode( "LD L,RLC (IY+$)"   , 23 ,  0 ), /* 05 */
			new OpCode( "RLC (IY+$)"        , 23 ,  0 ), /* 06 */
			new OpCode( "LD A,RLC (IY+$)"   , 23 ,  0 ), /* 07 */
			new OpCode( "LD B,RRC (IY+$)"   , 23 ,  0 ), /* 08 */
			new OpCode( "LD C,RRC (IY+$)"   , 23 ,  0 ), /* 09 */
			new OpCode( "LD D,RRC (IY+$)"   , 23 ,  0 ), /* 0A */
			new OpCode( "LD E,RRC (IY+$)"   , 23 ,  0 ), /* 0B */
			new OpCode( "LD H,RRC (IY+$)"   , 23 ,  0 ), /* 0C */
			new OpCode( "LD L,RRC (IY+$)"   , 23 ,  0 ), /* 0D */
			new OpCode( "RRC (IY+$)"        , 23 ,  0 ), /* 0E */
			new OpCode( "LD A,RRC (IY+$)"   , 23 ,  0 ), /* 0F */
			new OpCode( "LD B,RL (IY+$)"    , 23 ,  0 ), /* 10 */
			new OpCode( "LD C,RL (IY+$)"    , 23 ,  0 ), /* 11 */
			new OpCode( "LD D,RL (IY+$)"    , 23 ,  0 ), /* 12 */
			new OpCode( "LD E,RL (IY+$)"    , 23 ,  0 ), /* 13 */
			new OpCode( "LD H,RL (IY+$)"    , 23 ,  0 ), /* 14 */
			new OpCode( "LD L,RL (IY+$)"    , 23 ,  0 ), /* 15 */
			new OpCode( "RL (IY+$)"         , 23 ,  0 ), /* 16 */
			new OpCode( "LD A,RL (IY+$)"    , 23 ,  0 ), /* 17 */
			new OpCode( "LD B,RR (IY+$)"    , 23 ,  0 ), /* 18 */
			new OpCode( "LD C,RR (IY+$)"    , 23 ,  0 ), /* 19 */
			new OpCode( "LD D,RR (IY+$)"    , 23 ,  0 ), /* 1A */
			new OpCode( "LD E,RR (IY+$)"    , 23 ,  0 ), /* 1B */
			new OpCode( "LD H,RR (IY+$)"    , 23 ,  0 ), /* 1C */
			new OpCode( "LD L,RR (IY+$)"    , 23 ,  0 ), /* 1D */
			new OpCode( "RR (IY+$)"         , 23 ,  0 ), /* 1E */
			new OpCode( "LD A,RR (IY+$)"    , 23 ,  0 ), /* 1F */
			new OpCode( "LD B,SLA (IY+$)"   , 23 ,  0 ), /* 20 */
			new OpCode( "LD C,SLA (IY+$)"   , 23 ,  0 ), /* 21 */
			new OpCode( "LD D,SLA (IY+$)"   , 23 ,  0 ), /* 22 */
			new OpCode( "LD E,SLA (IY+$)"   , 23 ,  0 ), /* 23 */
			new OpCode( "LD H,SLA (IY+$)"   , 23 ,  0 ), /* 24 */
			new OpCode( "LD L,SLA (IY+$)"   , 23 ,  0 ), /* 25 */
			new OpCode( "SLA (IY+$)"        , 23 ,  0 ), /* 26 */
			new OpCode( "LD A,SLA (IY+$)"   , 23 ,  0 ), /* 27 */
			new OpCode( "LD B,SRA (IY+$)"   , 23 ,  0 ), /* 28 */
			new OpCode( "LD C,SRA (IY+$)"   , 23 ,  0 ), /* 29 */
			new OpCode( "LD D,SRA (IY+$)"   , 23 ,  0 ), /* 2A */
			new OpCode( "LD E,SRA (IY+$)"   , 23 ,  0 ), /* 2B */
			new OpCode( "LD H,SRA (IY+$)"   , 23 ,  0 ), /* 2C */
			new OpCode( "LD L,SRA (IY+$)"   , 23 ,  0 ), /* 2D */
			new OpCode( "SRA (IY+$)"        , 23 ,  0 ), /* 2E */
			new OpCode( "LD A,SRA (IY+$)"   , 23 ,  0 ), /* 2F */
			new OpCode( "LD B,SLL (IY+$)"   , 23 ,  0 ), /* 30 */
			new OpCode( "LD C,SLL (IY+$)"   , 23 ,  0 ), /* 31 */
			new OpCode( "LD D,SLL (IY+$)"   , 23 ,  0 ), /* 32 */
			new OpCode( "LD E,SLL (IY+$)"   , 23 ,  0 ), /* 33 */
			new OpCode( "LD H,SLL (IY+$)"   , 23 ,  0 ), /* 34 */
			new OpCode( "LD L,SLL (IY+$)"   , 23 ,  0 ), /* 35 */
			new OpCode( "SLL (IY+$)"        , 23 ,  0 ), /* 36 */
			new OpCode( "LD A,SLL (IY+$)"   , 23 ,  0 ), /* 37 */
			new OpCode( "LD B,SRL (IY+$)"   , 23 ,  0 ), /* 38 */
			new OpCode( "LD C,SRL (IY+$)"   , 23 ,  0 ), /* 39 */
			new OpCode( "LD D,SRL (IY+$)"   , 23 ,  0 ), /* 3A */
			new OpCode( "LD E,SRL (IY+$)"   , 23 ,  0 ), /* 3B */
			new OpCode( "LD H,SRL (IY+$)"   , 23 ,  0 ), /* 3C */
			new OpCode( "LD L,SRL (IY+$)"   , 23 ,  0 ), /* 3D */
			new OpCode( "SRL (IY+$)"        , 23 ,  0 ), /* 3E */
			new OpCode( "LD A,SRL (IY+$)"   , 23 ,  0 ), /* 3F */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 40 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 41 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 42 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 43 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 44 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 45 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 46 */
			new OpCode( "BIT 0,(IY+$)"      , 20 ,  0 ), /* 47 */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 48 */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 49 */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 4A */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 4B */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 4C */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 4D */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 4E */
			new OpCode( "BIT 1,(IY+$)"      , 20 ,  0 ), /* 4F */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 50 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 51 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 52 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 53 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 54 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 55 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 56 */
			new OpCode( "BIT 2,(IY+$)"      , 20 ,  0 ), /* 57 */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 58 */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 59 */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 5A */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 5B */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 5C */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 5D */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 5E */
			new OpCode( "BIT 3,(IY+$)"      , 20 ,  0 ), /* 5F */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 60 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 61 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 62 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 63 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 64 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 65 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 66 */
			new OpCode( "BIT 4,(IY+$)"      , 20 ,  0 ), /* 67 */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 68 */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 69 */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 6A */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 6B */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 6C */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 6D */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 6E */
			new OpCode( "BIT 5,(IY+$)"      , 20 ,  0 ), /* 6F */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 70 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 71 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 72 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 73 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 74 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 75 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 76 */
			new OpCode( "BIT 6,(IY+$)"      , 20 ,  0 ), /* 77 */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 78 */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 79 */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 7A */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 7B */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 7C */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 7D */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 7E */
			new OpCode( "BIT 7,(IY+$)"      , 20 ,  0 ), /* 7F */
			new OpCode( "LD B,RES 0,(IY+$)" , 23 ,  0 ), /* 80 */
			new OpCode( "LD C,RES 0,(IY+$)" , 23 ,  0 ), /* 81 */
			new OpCode( "LD D,RES 0,(IY+$)" , 23 ,  0 ), /* 82 */
			new OpCode( "LD E,RES 0,(IY+$)" , 23 ,  0 ), /* 83 */
			new OpCode( "LD H,RES 0,(IY+$)" , 23 ,  0 ), /* 84 */
			new OpCode( "LD L,RES 0,(IY+$)" , 23 ,  0 ), /* 85 */
			new OpCode( "RES 0,(IY+$)"      , 23 ,  0 ), /* 86 */
			new OpCode( "LD A,RES 0,(IY+$)" , 23 ,  0 ), /* 87 */
			new OpCode( "LD B,RES 1,(IY+$)" , 23 ,  0 ), /* 88 */
			new OpCode( "LD C,RES 1,(IY+$)" , 23 ,  0 ), /* 89 */
			new OpCode( "LD D,RES 1,(IY+$)" , 23 ,  0 ), /* 8A */
			new OpCode( "LD E,RES 1,(IY+$)" , 23 ,  0 ), /* 8B */
			new OpCode( "LD H,RES 1,(IY+$)" , 23 ,  0 ), /* 8C */
			new OpCode( "LD L,RES 1,(IY+$)" , 23 ,  0 ), /* 8D */
			new OpCode( "RES 1,(IY+$)"      , 23 ,  0 ), /* 8E */
			new OpCode( "LD A,RES 1,(IY+$)" , 23 ,  0 ), /* 8F */
			new OpCode( "LD B,RES 2,(IY+$)" , 23 ,  0 ), /* 90 */
			new OpCode( "LD C,RES 2,(IY+$)" , 23 ,  0 ), /* 91 */
			new OpCode( "LD D,RES 2,(IY+$)" , 23 ,  0 ), /* 92 */
			new OpCode( "LD E,RES 2,(IY+$)" , 23 ,  0 ), /* 93 */
			new OpCode( "LD H,RES 2,(IY+$)" , 23 ,  0 ), /* 94 */
			new OpCode( "LD L,RES 2,(IY+$)" , 23 ,  0 ), /* 95 */
			new OpCode( "RES 2,(IY+$)"      , 23 ,  0 ), /* 96 */
			new OpCode( "LD A,RES 2,(IY+$)" , 23 ,  0 ), /* 97 */
			new OpCode( "LD B,RES 3,(IY+$)" , 23 ,  0 ), /* 98 */
			new OpCode( "LD C,RES 3,(IY+$)" , 23 ,  0 ), /* 99 */
			new OpCode( "LD D,RES 3,(IY+$)" , 23 ,  0 ), /* 9A */
			new OpCode( "LD E,RES 3,(IY+$)" , 23 ,  0 ), /* 9B */
			new OpCode( "LD H,RES 3,(IY+$)" , 23 ,  0 ), /* 9C */
			new OpCode( "LD L,RES 3,(IY+$)" , 23 ,  0 ), /* 9D */
			new OpCode( "RES 3,(IY+$)"      , 23 ,  0 ), /* 9E */
			new OpCode( "LD A,RES 3,(IY+$)" , 23 ,  0 ), /* 9F */
			new OpCode( "LD B,RES 4,(IY+$)" , 23 ,  0 ), /* A0 */
			new OpCode( "LD C,RES 4,(IY+$)" , 23 ,  0 ), /* A1 */
			new OpCode( "LD D,RES 4,(IY+$)" , 23 ,  0 ), /* A2 */
			new OpCode( "LD E,RES 4,(IY+$)" , 23 ,  0 ), /* A3 */
			new OpCode( "LD H,RES 4,(IY+$)" , 23 ,  0 ), /* A4 */
			new OpCode( "LD L,RES 4,(IY+$)" , 23 ,  0 ), /* A5 */
			new OpCode( "RES 4,(IY+$)"      , 23 ,  0 ), /* A6 */
			new OpCode( "LD A,RES 4,(IY+$)" , 23 ,  0 ), /* A7 */
			new OpCode( "LD B,RES 5,(IY+$)" , 23 ,  0 ), /* A8 */
			new OpCode( "LD C,RES 5,(IY+$)" , 23 ,  0 ), /* A9 */
			new OpCode( "LD D,RES 5,(IY+$)" , 23 ,  0 ), /* AA */
			new OpCode( "LD E,RES 5,(IY+$)" , 23 ,  0 ), /* AB */
			new OpCode( "LD H,RES 5,(IY+$)" , 23 ,  0 ), /* AC */
			new OpCode( "LD L,RES 5,(IY+$)" , 23 ,  0 ), /* AD */
			new OpCode( "RES 5,(IY+$)"      , 23 ,  0 ), /* AE */
			new OpCode( "LD A,RES 5,(IY+$)" , 23 ,  0 ), /* AF */
			new OpCode( "LD B,RES 6,(IY+$)" , 23 ,  0 ), /* B0 */
			new OpCode( "LD C,RES 6,(IY+$)" , 23 ,  0 ), /* B1 */
			new OpCode( "LD D,RES 6,(IY+$)" , 23 ,  0 ), /* B2 */
			new OpCode( "LD E,RES 6,(IY+$)" , 23 ,  0 ), /* B3 */
			new OpCode( "LD H,RES 6,(IY+$)" , 23 ,  0 ), /* B4 */
			new OpCode( "LD L,RES 6,(IY+$)" , 23 ,  0 ), /* B5 */
			new OpCode( "RES 6,(IY+$)"      , 23 ,  0 ), /* B6 */
			new OpCode( "LD A,RES 6,(IY+$)" , 23 ,  0 ), /* B7 */
			new OpCode( "LD B,RES 7,(IY+$)" , 23 ,  0 ), /* B8 */
			new OpCode( "LD C,RES 7,(IY+$)" , 23 ,  0 ), /* B9 */
			new OpCode( "LD D,RES 7,(IY+$)" , 23 ,  0 ), /* BA */
			new OpCode( "LD E,RES 7,(IY+$)" , 23 ,  0 ), /* BB */
			new OpCode( "LD H,RES 7,(IY+$)" , 23 ,  0 ), /* BC */
			new OpCode( "LD L,RES 7,(IY+$)" , 23 ,  0 ), /* BD */
			new OpCode( "RES 7,(IY+$)"      , 23 ,  0 ), /* BE */
			new OpCode( "LD A,RES 7,(IY+$)" , 23 ,  0 ), /* BF */
			new OpCode( "LD B,SET 0,(IY+$)" , 23 ,  0 ), /* C0 */
			new OpCode( "LD C,SET 0,(IY+$)" , 23 ,  0 ), /* C1 */
			new OpCode( "LD D,SET 0,(IY+$)" , 23 ,  0 ), /* C2 */
			new OpCode( "LD E,SET 0,(IY+$)" , 23 ,  0 ), /* C3 */
			new OpCode( "LD H,SET 0,(IY+$)" , 23 ,  0 ), /* C4 */
			new OpCode( "LD L,SET 0,(IY+$)" , 23 ,  0 ), /* C5 */
			new OpCode( "SET 0,(IY+$)"      , 23 ,  0 ), /* C6 */
			new OpCode( "LD A,SET 0,(IY+$)" , 23 ,  0 ), /* C7 */
			new OpCode( "LD B,SET 1,(IY+$)" , 23 ,  0 ), /* C8 */
			new OpCode( "LD C,SET 1,(IY+$)" , 23 ,  0 ), /* C9 */
			new OpCode( "LD D,SET 1,(IY+$)" , 23 ,  0 ), /* CA */
			new OpCode( "LD E,SET 1,(IY+$)" , 23 ,  0 ), /* CB */
			new OpCode( "LD H,SET 1,(IY+$)" , 23 ,  0 ), /* CC */
			new OpCode( "LD L,SET 1,(IY+$)" , 23 ,  0 ), /* CD */
			new OpCode( "SET 1,(IY+$)"      , 23 ,  0 ), /* CE */
			new OpCode( "LD A,SET 1,(IY+$)" , 23 ,  0 ), /* CF */
			new OpCode( "LD B,SET 2,(IY+$)" , 23 ,  0 ), /* D0 */
			new OpCode( "LD C,SET 2,(IY+$)" , 23 ,  0 ), /* D1 */
			new OpCode( "LD D,SET 2,(IY+$)" , 23 ,  0 ), /* D2 */
			new OpCode( "LD E,SET 2,(IY+$)" , 23 ,  0 ), /* D3 */
			new OpCode( "LD H,SET 2,(IY+$)" , 23 ,  0 ), /* D4 */
			new OpCode( "LD L,SET 2,(IY+$)" , 23 ,  0 ), /* D5 */
			new OpCode( "SET 2,(IY+$)"      , 23 ,  0 ), /* D6 */
			new OpCode( "LD A,SET 2,(IY+$)" , 23 ,  0 ), /* D7 */
			new OpCode( "LD B,SET 3,(IY+$)" , 23 ,  0 ), /* D8 */
			new OpCode( "LD C,SET 3,(IY+$)" , 23 ,  0 ), /* D9 */
			new OpCode( "LD D,SET 3,(IY+$)" , 23 ,  0 ), /* DA */
			new OpCode( "LD E,SET 3,(IY+$)" , 23 ,  0 ), /* DB */
			new OpCode( "LD H,SET 3,(IY+$)" , 23 ,  0 ), /* DC */
			new OpCode( "LD L,SET 3,(IY+$)" , 23 ,  0 ), /* DD */
			new OpCode( "SET 3,(IY+$)"      , 23 ,  0 ), /* DE */
			new OpCode( "LD A,SET 3,(IY+$)" , 23 ,  0 ), /* DF */
			new OpCode( "LD B,SET 4,(IY+$)" , 23 ,  0 ), /* E0 */
			new OpCode( "LD C,SET 4,(IY+$)" , 23 ,  0 ), /* E1 */
			new OpCode( "LD D,SET 4,(IY+$)" , 23 ,  0 ), /* E2 */
			new OpCode( "LD E,SET 4,(IY+$)" , 23 ,  0 ), /* E3 */
			new OpCode( "LD H,SET 4,(IY+$)" , 23 ,  0 ), /* E4 */
			new OpCode( "LD L,SET 4,(IY+$)" , 23 ,  0 ), /* E5 */
			new OpCode( "SET 4,(IY+$)"      , 23 ,  0 ), /* E6 */
			new OpCode( "LD A,SET 4,(IY+$)" , 23 ,  0 ), /* E7 */
			new OpCode( "LD B,SET 5,(IY+$)" , 23 ,  0 ), /* E8 */
			new OpCode( "LD C,SET 5,(IY+$)" , 23 ,  0 ), /* E9 */
			new OpCode( "LD D,SET 5,(IY+$)" , 23 ,  0 ), /* EA */
			new OpCode( "LD E,SET 5,(IY+$)" , 23 ,  0 ), /* EB */
			new OpCode( "LD H,SET 5,(IY+$)" , 23 ,  0 ), /* EC */
			new OpCode( "LD L,SET 5,(IY+$)" , 23 ,  0 ), /* ED */
			new OpCode( "SET 5,(IY+$)"      , 23 ,  0 ), /* EE */
			new OpCode( "LD A,SET 5,(IY+$)" , 23 ,  0 ), /* EF */
			new OpCode( "LD B,SET 6,(IY+$)" , 23 ,  0 ), /* F0 */
			new OpCode( "LD C,SET 6,(IY+$)" , 23 ,  0 ), /* F1 */
			new OpCode( "LD D,SET 6,(IY+$)" , 23 ,  0 ), /* F2 */
			new OpCode( "LD E,SET 6,(IY+$)" , 23 ,  0 ), /* F3 */
			new OpCode( "LD H,SET 6,(IY+$)" , 23 ,  0 ), /* F4 */
			new OpCode( "LD L,SET 6,(IY+$)" , 23 ,  0 ), /* F5 */
			new OpCode( "SET 6,(IY+$)"      , 23 ,  0 ), /* F6 */
			new OpCode( "LD A,SET 6,(IY+$)" , 23 ,  0 ), /* F7 */
			new OpCode( "LD B,SET 7,(IY+$)" , 23 ,  0 ), /* F8 */
			new OpCode( "LD C,SET 7,(IY+$)" , 23 ,  0 ), /* F9 */
			new OpCode( "LD D,SET 7,(IY+$)" , 23 ,  0 ), /* FA */
			new OpCode( "LD E,SET 7,(IY+$)" , 23 ,  0 ), /* FB */
			new OpCode( "LD H,SET 7,(IY+$)" , 23 ,  0 ), /* FC */
			new OpCode( "LD L,SET 7,(IY+$)" , 23 ,  0 ), /* FD */
			new OpCode( "SET 7,(IY+$)"      , 23 ,  0 ), /* FE */
			new OpCode( "LD A,SET 7,(IY+$)" , 23 ,  0 ), /* FF */

		};
	}
}
