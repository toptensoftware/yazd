using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace yazd
{
	class Program
	{
		string _inputFile;
		string _outputFile;
		int _baseAddr=0;
		int _header=0;
		int _start = 0;
		int _len = 0;
		bool _xref = false;
		bool _lst = false;
		bool _lowerCase = false;
		bool _markWordRefs = false;
		List<int> _entryPoints = new List<int>();

		int _addrSpaceStart;
		int _addrSpaceEnd;

		void CheckAddress(int a)
		{
			if (a < 0 || a > 0xFFFF)
			{
				throw new InvalidOperationException(string.Format("Address 0x{0:X} is out of range", a));
			}

			if (a < _addrSpaceStart || a > _addrSpaceEnd)
			{
				throw new InvalidOperationException(string.Format("Address 0x{0:X4} is out of range 0x{1:X4}-0x{2:X4}", a, _addrSpaceStart, _addrSpaceEnd));
			}
		}

		public bool ProcessArg(string arg)
		{
			if (arg == null)
				return true;

			if (arg.StartsWith("#"))
				return true;

			// Response file
			if (arg.StartsWith("@"))
			{
				// Get the fully qualified response file name
				string strResponseFile = System.IO.Path.GetFullPath(arg.Substring(1));

				// Load and parse the response file
				var args = Utils.ParseCommandLine(System.IO.File.ReadAllText(strResponseFile));

				// Set the current directory
				string OldCurrentDir = System.IO.Directory.GetCurrentDirectory();
				System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(strResponseFile));

				// Load the file
				bool bRetv = ProcessArgs(args);

				// Restore current directory
				System.IO.Directory.SetCurrentDirectory(OldCurrentDir);

				return bRetv;
			}

			// Args are in format [/-]<switchname>[:<value>];
			if (arg.StartsWith("/") || arg.StartsWith("-"))
			{
				string SwitchName = arg.Substring(arg.StartsWith("--") ? 2 : 1);
				string Value = null;

				int colonpos = SwitchName.IndexOf(':');
				if (colonpos >= 0)
				{
					// Split it
					Value = SwitchName.Substring(colonpos + 1);
					SwitchName = SwitchName.Substring(0, colonpos);
				}

				switch (SwitchName)
				{
					case "help":
					case "h":
					case "?":
						ShowLogo();
						ShowHelp();
						return false;

					case "v":
						ShowLogo();
						return false;

					case "addr":
						_baseAddr = Utils.ParseUShort(Value);
						if (_start == 0)
							_start = _baseAddr;
						break;

					case "header":
						_header = Utils.ParseUShort(Value);
						break;

					case "start":
						_start = Utils.ParseUShort(Value);
						break;

					case "end":
						_len = Utils.ParseUShort(Value) - _start;
						break;

					case "len":
						_len = Utils.ParseUShort(Value);
						break;

					case "entry":
						_entryPoints.Add(Utils.ParseUShort(Value));
						break;

					case "xref":
						_xref = true;
						break;

					case "lst":
						_lst = true;
						break;

					case "lc":
					case "lowercase":
						_lowerCase = true;
						break;

					case "mwr":
					case "markwordrefs":
						_markWordRefs = true;
						break;

					default:
						throw new InvalidOperationException(string.Format("Unknown switch '{0}'", arg));
				}
			}
			else
			{
				if (_inputFile == null)
					_inputFile = arg;
				else if (_outputFile == null)
					_outputFile = arg;
				else
					throw new InvalidOperationException(string.Format("Too many command line arguments, don't know what to do with '{0}'", arg));
			}

			return true;
		}

		public bool ProcessArgs(IEnumerable<string> args)
		{
			if (args == null)
				return true;

			// Parse args
			foreach (var a in args)
			{
				if (!ProcessArg(a))
					return false;
			}

			return true;
		}


		public void ShowLogo()
		{
			System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Console.WriteLine("yazd v{0} - Z80 Disassembler", v);
			Console.WriteLine("Copyright (C) 2012 Topten Software. All Rights Reserved.");
			Console.WriteLine("");
		}

		public void ShowHelp()
		{
			Console.WriteLine("usage: yazd source.bin destination.asm [options]");
			Console.WriteLine();

			Console.WriteLine("Options:");
			Console.WriteLine("  --header:N             Skip N header bytes at start of file");
			Console.WriteLine("  --addr:N               Z-80 base address (of first byte after header)");
			Console.WriteLine("  --start:N              Z-80 address to disassemble from");
			Console.WriteLine("  --end:N                Z-80 address to stop at");
			Console.WriteLine("  --len:N                or, number of bytes to disassamble");
			Console.WriteLine("  --xref                 include referenced locations of labels");
			Console.WriteLine("  --lst                  generate a listing file (more detail, can't be assembled)");
			Console.WriteLine("  --lowercase|lc         render mnemonics in lowercase");
			Console.WriteLine("  --markwordrefs|mwr     highlight literal word values that may be an address with a comment");
			Console.WriteLine("  --help                 Show these help instruction");
			Console.WriteLine("  --v                    Show version information");

			Console.WriteLine();
			Console.WriteLine("Response file containing arguments can be specified with @ prefix on file name");

			Console.WriteLine("");
		}

		public int Run(string[] args)
		{
			// Process command line
			ProcessArgs(args);

			// Open input file
			var code = System.IO.File.ReadAllBytes(_inputFile);

			// Work out the available address space
			_addrSpaceStart = _baseAddr;
			_addrSpaceEnd = (ushort)(_baseAddr + (code.Length - _header));

			// Work out auto length
			if (_len == 0)
				_len = (ushort)(code.Length - _header);

			// Check specified address range
			CheckAddress(_start);
			CheckAddress(_start + _len);
			foreach (var addr in _entryPoints)
				CheckAddress(addr);

			// Setup disassembler parameters
			Disassembler.LabelledRangeLow = (ushort)_start;
			Disassembler.LabelledRangeHigh = (ushort)(_start + _len);
			Disassembler.LowerCase = _lowerCase;

			// Disassemble
			var instructions = new Dictionary<int, Disassembler.Instruction>();
			if (_entryPoints.Count > 0)
			{
				var pendingCodePaths = new List<int>();
				pendingCodePaths.AddRange(_entryPoints);

				while (pendingCodePaths.Count > 0)
				{
					// Get a new address that needs disassembling
					int addr = pendingCodePaths[0];
					pendingCodePaths.RemoveAt(0);

					// Disassemble
					while (!instructions.ContainsKey(addr) && addr >= _start && addr < _start + _len)
					{
						// Disassemble this instruction
						var i = Disassembler.Disassemble(code, (ushort)(addr - _baseAddr - _header), (ushort)addr);

						// Possible address reference?
						if (_markWordRefs && i.word_val.HasValue && (i.opCode.flags & (OpCodeFlags.Jumps | OpCodeFlags.RefAddr)) == 0)
						{
							i.Comment = "address or value?";
						}

						// Add it
						instructions.Add(addr, i);

						// If have a jump address, dump it
						if (i.next_addr_2.HasValue)
						{
							pendingCodePaths.Add(i.next_addr_2.Value);
						}

						// Continue
						if (i.next_addr_1.HasValue)
							addr = i.next_addr_1.Value;
						else
							break;

					}
				}
			}
			else
			{
				// Linear disassembly
				for (int addr = _start; addr < _start + _len; )
				{
					// Disassemble this instruction
					var i = Disassembler.Disassemble(code, (ushort)(addr - _baseAddr - _header), (ushort)addr);

					// Add it
					instructions.Add(addr, i);

					// Update address
					addr += i.bytes;
				}
			}

			// Sort all instructions
			var sorted = instructions.Values.OrderBy(x => x.addr).ToList();

			// Helper for generating DB directives
			Func<int, int, int, int> FillData = delegate(int from, int to, int index)
			{
				for (int j = from; j < to; j++)
				{
					var data = code[j - _baseAddr - _header];

					// Get the byte
					var instruction = new Disassembler.Instruction();
					if (data >= 0x20 && data <= 0x7f)
						instruction.Comment = string.Format("'{0}'", (char)data);
					instruction.addr = (ushort)j;
					instruction.Asm = string.Format("DB {0}", Disassembler.FormatByte(data));
					instruction.next_addr_1 = (ushort)(j + 1);
					instruction.bytes = 1;

					// Add to instruction map
					instructions.Add(instruction.addr, instruction);

					// Add to sorted list
					sorted.Insert(index, instruction);
					index++;
				}
				return index;
			};

			// Fill in all unpopulated areas with DBs
			int expectedNextAddress = _start;
			for (int i = 0; i < sorted.Count; i++)
			{
				var inst = sorted[i];
				if (inst.addr != expectedNextAddress)
				{
					i = FillData(expectedNextAddress, inst.addr, i);
				}

				expectedNextAddress = sorted[i].addr + sorted[i].bytes;
			}
			FillData(expectedNextAddress, _start + _len, instructions.Count);

			// Mark all entry points
			foreach (var e in _entryPoints)
			{
				instructions[e].entryPoint = true;
			}

			// Resolve references
			foreach (var i in instructions)
			{
				ushort? ref_addr = i.Value.next_addr_2;
				if (!ref_addr.HasValue)
				{
					if (i.Value.word_val.HasValue && (i.Value.opCode.flags & OpCodeFlags.RefAddr) != 0)
						ref_addr = i.Value.word_val;
				}


				if (ref_addr.HasValue)
				{
					Disassembler.Instruction target;
					if (instructions.TryGetValue(ref_addr.Value, out target))
					{
						if (target.referencedFrom == null)
							target.referencedFrom = new List<Disassembler.Instruction>();
						target.referencedFrom.Add(i.Value);
					}
				}
			}

			// Write out the "ORG" directive
			if (sorted.Count > 0 && !_lst)
			{
				Console.WriteLine("\n\tORG\t{0}\n", Disassembler.FormatWord(sorted[0].addr));
			}

			TabbedTextWriter w = new TabbedTextWriter(Console.Out);
			if (_lst)
			{
				w.TabStops = new int[] { 32, 40, 48, 56, 64 };
			}
			else
			{
				w.TabStops = new int[] { 8, 16, 32 };
			}

			// List it
			Disassembler.Instruction prev = null;
			foreach (var i in sorted)
			{
				// Include cross references?
				if (_xref && i.referencedFrom != null)
				{
					// Ensure a blank line before reference comments
					if (prev != null && prev.next_addr_1.HasValue)
					{
						w.WriteLine();
					}

					if (_lst)
						w.Write("\t");

					w.WriteLine("\t; Referenced from {0}",
							string.Join(", ", i.referencedFrom.Select(x => Disassembler.FormatWord(x.addr)).ToList()));
				}

				if (i.entryPoint)
				{
					if (_lst)
						w.Write("\t");
					w.WriteLine("\t; Entry Point");
				}

				if (_lst)
				{
					w.Write("{0:X4}:", i.addr);
					for (int j = 0; j < i.bytes; j++)
					{
						var data = code[i.addr + j - _baseAddr - _header];
						w.Write(" {0:X2}", data);
					}
					w.Write("\t ");
				}

				// Work out label
				string label = "";
				if (i.entryPoint || i.referencedFrom != null || (prev != null && !prev.next_addr_1.HasValue))
					label = Disassembler.FormatAddr(i.addr);

				// Write the disassembled instruction
				w.Write("{0}\t{1}", label, i.Asm.Replace(" ", "\t"));

				// Write out an optional comment
				if (i.Comment != null)
					w.WriteLine("\t; {0}", i.Comment);
				else
					w.WriteLine();

				// If this instruction doesn't continue on, insert a blank line
				if (!i.next_addr_1.HasValue)
				{
					w.WriteLine();
				}

				// Remember the previous instruction
				prev = i;
			}

			if (!_lst)
				return 0;

			// Build a list of all possible address references
			Dictionary<int, AddressInfo> addressInfos = new Dictionary<int,AddressInfo>();
			Dictionary<int, PortInfo> portInfos = new Dictionary<int, PortInfo>();
			foreach (var i in sorted)
			{
				// Does this instruction reference a word value?
				if (i.word_val.HasValue)
				{
					AddressInfo ai;
					if (!addressInfos.TryGetValue(i.word_val.Value, out ai))
					{
						ai = new AddressInfo(i.word_val.Value);
						addressInfos.Add(ai.addr, ai);
					}

					if ((i.opCode.flags & OpCodeFlags.RefAddr)!=0)
					{
						// Known referenced data address
						ai.DataReferences.Add(i);
					}

					if ((i.opCode.flags & OpCodeFlags.Jumps) !=0)
					{
						// Known referenced code address
						ai.CodeReferences.Add(i);
					}

					if ((i.opCode.flags & (OpCodeFlags.Jumps | OpCodeFlags.RefAddr)) == 0)
					{
						// Potential address
						ai.PotentialReferences.Add(i);
					}
				}

				// Is it a port reference?
				if (i.opCode != null && (i.opCode.flags & OpCodeFlags.PortRef) != 0)
				{
					// Which port (-1, referenced through a register)
					int port = -1;
					if (i.byte_val.HasValue)
						port = i.byte_val.Value;
					
					// Get the port info
					PortInfo pi;
					if (!portInfos.TryGetValue(port, out pi))
					{
						pi = new PortInfo(port);
						portInfos.Add(port, pi);
					}

					pi.References.Add(i);
				}
			}

			w.TabStops = new int[] { 8, 16, 24, 32 };

			// Build a list of all external references
			var extRefs = addressInfos.Values.Where(x => x.addr < _start || x.addr >= _start + _len).OrderBy(x=>x.addr);
			foreach (var r in extRefs)
			{
				if (r.DataReferences.Count > 0 || r.CodeReferences.Count > 0)
				{ 
					w.WriteLine("\nreferences to external address {0:X4}:", r.addr);
					
					foreach (var i in (r.DataReferences.Concat(r.CodeReferences).Concat(r.PotentialReferences)).OrderBy(x=>x.addr))
					{
						w.WriteLine("\t{0:X4} {1}", i.addr, i.Asm);
					}

				}
			}

			foreach (var r in addressInfos.Values.Where(x => (x.addr >= _start && x.addr < _start + _len) && x.PotentialReferences.Count > 0).OrderBy(x => x.addr))
			{
				w.WriteLine("\npossible references to internal address {0:X4}:", r.addr);
				ListPotentialAddresses(w, r);
			}

			foreach (var r in addressInfos.Values.Where(x => (x.addr < _start || x.addr >= _start + _len) && x.PotentialReferences.Count > 0).OrderBy(x => x.addr))
			{
				w.WriteLine("\npossible references to external address {0:X4}:", r.addr);
				ListPotentialAddresses(w, r);
			}

			foreach (var r in portInfos.Values.OrderBy(x => x.port))
			{
				if (r.port == -1)
				{
					w.WriteLine("\nport references through a register:");
				}
				else
				{
					w.WriteLine("\nreferences to port {0}", Disassembler.FormatByte((byte)r.port));
				}

				foreach (var i in r.References.OrderBy(x=>x.opCode.mnemonic[0]).ThenBy(x => x.addr))
				{
					w.WriteLine("\t{0:X4} {1}", i.addr, i.Asm);
				}

			}




			return 0;
		}

		private static void ListPotentialAddresses(TabbedTextWriter w, AddressInfo r)
		{
			foreach (var i in r.PotentialReferences.OrderBy(x => x.addr))
			{
				w.WriteLine("\t{0:X4} {1}", i.addr, i.Asm);
			}

			bool bFirstOtherRef = true;
			foreach (var i in r.CodeReferences.Concat(r.DataReferences).OrderBy(x => x.addr))
			{
				if (bFirstOtherRef)
					w.WriteLine("\t----------");
				bFirstOtherRef = false;

				w.WriteLine("\t{0:X4} {1}", i.addr, i.Asm);
			}
		}

		class PortInfo
		{
			public PortInfo(int port)
			{
				this.port = port;
			}
			public int port;
			public List<Disassembler.Instruction> References = new List<Disassembler.Instruction>();
		}

		class AddressInfo
		{
			public AddressInfo(int addr)
			{
				this.addr = addr;
			}
			public int addr;
			public List<Disassembler.Instruction> CodeReferences = new List<Disassembler.Instruction>();
			public List<Disassembler.Instruction> DataReferences = new List<Disassembler.Instruction>();
			public List<Disassembler.Instruction> PotentialReferences = new List<Disassembler.Instruction>();
		}



		static int Main(string[] args)
		{
			try
			{
				return new Program().Run(args);
			}
				/*
			catch (InvalidOperationException x)
			{
				Console.WriteLine("{0}", x.Message);
				return 7;
			}
				 */
			catch (IOException x)
			{
				Console.WriteLine("File Error - {0}", x.Message);
				return 7;
			}
		}
	}
}
