﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yazd
{
	class CallGraphAnalyzer
	{
		public CallGraphAnalyzer(Dictionary<int, Instruction> Instructions, List<Instruction> Sorted)
		{
			_instructions = Instructions;
			_sorted = Sorted;
		}

		Dictionary<int, Instruction> _instructions;
		List<Instruction> _sorted;
		Dictionary<int, Proc> _procs = new Dictionary<int, Proc>();

		public Dictionary<int, Proc> Analyse()
		{

			// Create a proc for every entry point
			foreach (var i in _sorted.Where(x => x.entryPoint))
			{
				if (!_procs.ContainsKey(i.addr))
					_procs.Add(i.addr, new Proc(i));
			}

			// Locate all CALL instructions and create procs for the target locations
			foreach (var i in _sorted.Where(i => i.opCode != null && (i.opCode.flags & OpCodeFlags.Call)!=0))
			{
				if (i.next_addr_2.HasValue)
				{
					// Find the proc
					if (!_procs.ContainsKey(i.next_addr_2.Value))
					{
						// Find the target instruction
						Instruction targetInstruction;
						if (_instructions.TryGetValue(i.next_addr_2.Value, out targetInstruction))
						{
							_procs.Add(targetInstruction.addr, new Proc(targetInstruction));
						}
					}
				}
			}

			// Analyse each proc
			var procList = _procs.Values.ToList();
			for (int i=0; i<procList.Count; i++)
			{
				// Analyse this proc
				var newProcs = AnalyseProc(procList[i], procList);
			}

			// Work out if the proc ever returns
			return _procs;
		}

		List<Proc> AnalyseProc(Proc p, List<Proc> procList)
		{
			Instruction i = p.firstInstruction;
			List<int> dependants= new List<int>();
			while (true)
			{
				// Update the length of this proc
				p.lengthInBytes = i.addr - p.firstInstruction.addr + i.bytes;

				// If this is a return instruction, mark the proc as having at least one return
				if (i.opCode != null && (i.opCode.flags & OpCodeFlags.Returns)!=0)
				{
					p.hasLocalReturn = true;
				}

				// Have we reached the start of another procedure?
				if (i.proc != null && i.proc != p)
				{
					// Yes! procedure falls through into another, treat as a tail call into that proc
					if (!dependants.Contains(i.addr))
						dependants.Add(i.addr);
					break;
				}

				// Remove any dependants that are within this proc (generated by local forward jumps)
				dependants.RemoveAll(x => x >= p.firstInstruction.addr && x < i.addr);

				// Does this instruction transfer control to somewhere else?
				if (i.next_addr_2.HasValue)
				{
					// if transfers to before start of this proc, or after the current location
					// then mark it as a dependant location
					if (i.next_addr_2.Value < p.firstInstruction.addr ||
								i.next_addr_2.Value > i.addr)
					{
						if (!dependants.Contains(i.next_addr_2.Value))
							dependants.Add(i.next_addr_2.Value);
					}
				}

				// Does this instruction continue?
				if (!i.next_addr_1.HasValue)
				{
					// No, see if the next dependant address can be reached without hitting another proc
					Instruction instContinue = null;
					List<int> forwardDependants = dependants.Where(x => x > i.addr).ToList();
					if (forwardDependants.Count>0)
					{
						int nextDependantAddress = forwardDependants.Min();
						for (int j = i.addr+1; j <= nextDependantAddress; j++)
						{
							// Get the instruction at that address, ignore if none
							Instruction i2 = null;
							if (!_instructions.TryGetValue(j, out i2))
								continue;

							// Skip data instructions
							if (i2.opCode == null)
								continue;

							// Quit if we hit another proc
							if (i2.proc != null)
								break;

							// Have we reached the next dependant location?
							if (i2.addr == nextDependantAddress)
							{
								// Yes, continue from here
								instContinue = i2;
							}
						}
					}

					if (instContinue != null)
					{
						i = instContinue;
						continue;
					}
					else
					{
						break;
					}
				}

				// Get the next instruction
				if (!_instructions.TryGetValue(i.next_addr_1.Value, out i))
				{
					// ie: fall through to external location!
					break;
				}
			}

			// Store the dependants with the proc
			p.dependants = dependants;

			// Create new procs for any dependants
			foreach (var addr in dependants.Where(x=>!_procs.ContainsKey(x)))
			{
				// Get the dependant instruction, ignore if external
				Instruction dependantInstruction;
				if (!_instructions.TryGetValue(addr, out dependantInstruction))
					continue;

				// Ignore if already a proc there
				if (dependantInstruction.proc!=null)
					continue;

				// Create new proc
				var newProc = new Proc(dependantInstruction);

				// Add to the list of procs still to be analyzed
				_procs.Add(addr, newProc);
				procList.Add(newProc);
			}

			return null;
		}
	}
}
