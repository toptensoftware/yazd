using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yazd
{
	public class Instruction
	{
		public string Asm;
		public string Comment;
		public int t_states;
		public int t_states2;
		public ushort bytes;
		public ushort addr;
		public ushort? next_addr_1;
		public ushort? next_addr_2;
		public ushort? word_val;
		public byte? byte_val;
		public List<Instruction> referencedFrom;
		public OpCode opCode;
		public bool entryPoint;
		public Proc proc;
	}

}
