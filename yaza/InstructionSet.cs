using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yazd;

namespace yaza
{
    public static class InstructionSet
    {
        // Static constructor - initialize the instruction set table
        static InstructionSet()
        {
            ProcessOpCodeTable(OpCodes.dasm_base, new byte[0]);
            ProcessOpCodeTable(OpCodes.dasm_ed, new byte[] { 0xED });
            ProcessOpCodeTable(OpCodes.dasm_cb, new byte[] { 0xCB });
            ProcessOpCodeTable(OpCodes.dasm_dd, new byte[] { 0xDD });
            ProcessOpCodeTable(OpCodes.dasm_fd, new byte[] { 0xFD });
            ProcessOpCodeTable(OpCodes.dasm_ddcb, new byte[] { 0xDD, 0xCB }, true);
            ProcessOpCodeTable(OpCodes.dasm_fdcb, new byte[] { 0xFD, 0xCB }, true);

            // Build a list of all instruction names
            _instructionNames = new HashSet<string>(_opMap.Values.Select(x => x.InstructionName), StringComparer.InvariantCultureIgnoreCase);
        }


        // Given a mnemonic pattern, find the associated instruction
        public static Instruction Find(string mnemonic)
        {
            Instruction op;
            if (!_opMap.TryGetValue(mnemonic, out op))
                return null;
            return op;
        }



        // Dictionary of mnemonic to instruction group or instruction definition
        static Dictionary<string, Instruction> _opMap = new Dictionary<string, Instruction>(StringComparer.InvariantCultureIgnoreCase);

        // Convert a type qualified mnemonic into a look up key
        static string KeyOfMnemonic(string mnemonic, out int? immValue)
        {
            // Default to no immediate value
            immValue = null;

            // Start after the instruction name
            var spacePos = mnemonic.IndexOf(' ');
            if (spacePos < 0)
                return mnemonic;

            // Build the new mnemonic
            var sb = new StringBuilder();
            sb.Append(mnemonic.Substring(0, spacePos));

            for (int i = spacePos; i < mnemonic.Length; i++)
            {
                // Get the character
                var ch = mnemonic[i];

                // Typed placeholder?
                if (ch == '%' || ch == '@' || ch == '#' || ch == '$')
                {
                    sb.Append('?');
                    continue;
                }

                // Immediate value
                if (ch >= '0' && ch <= '9')
                {
                    sb.Append('?');

                    // Extract the immediate value
                    int pos = i;
                    while (i < mnemonic.Length && ((mnemonic[i] >= '0' && mnemonic[i] <= '9') || mnemonic[i] == 'x'))
                        i++;
                    var immString = mnemonic.Substring(pos, i - pos);

                    // Parse the immediate value
                    if (immString.StartsWith("0x"))
                    {
                        immValue = Convert.ToInt32(immString.Substring(2), 16);
                    }
                    else
                    {
                        immValue = Convert.ToInt32(immString);
                    }

                    i--;
                    continue;
                }

                sb.Append(ch);
            }

            return sb.ToString();
        }


        // Add an instruction to the set of available instructions
        static void AddInstruction(InstructionDefinition instruction)
        {
            // Prepare the instruction
            instruction.Prepare();

            // Get it's key, replacing operand placeholders with '?'
            int? immValue;
            string key = KeyOfMnemonic(instruction.mnemonic, out immValue);

            // Get the existing instruction
            Instruction existing;
            _opMap.TryGetValue(key, out existing);

            // Does it need to go into an instruction group?
            if (immValue.HasValue)
            {
                if (existing != null)
                {
                    // Use existing group
                    var group = existing as InstructionGroup;
                    if (group == null)
                    {
                        throw new InvalidOperationException("Internal error: instruction overloaded with typed imm and bit imm");
                    }

                    group.AddInstructionDefinition(immValue.Value, instruction);
                }
                else
                {
                    // Create new instruction group
                    var group = new InstructionGroup();
                    group.mnemonic = key;
                    group.AddInstructionDefinition(immValue.Value, instruction);
                    _opMap.Add(key, group);
                }
            }
            else
            {
                if (existing != null)                 
                {
                    if (existing is InstructionGroup)
                    {
                        throw new InvalidOperationException("Internal error: instruction overloaded with typed imm and bit imm");
                    }

                    /*
                    if (existing.opCode.mnemonic != instruction.opCode.mnemonic)
                    {
                        Console.WriteLine("Arg overload: {0}", instruction.opCode.mnemonic);
                    }
                    else
                    {
                        Console.WriteLine("Duplicate mnemonic: {0}", instruction.opCode.mnemonic);
                    }
                    */
                }
                else
                {
                    _opMap.Add(key, instruction);
                }
            }
        }

        static void ProcessOpCodeTable(OpCode[] table, byte[] prefixBytes, bool opIsSuffix = false)
        {
            for (int i = 0; i < table.Length; i++)
            {
                var opCode = table[i];
                if (opCode.mnemonic == null || opCode.mnemonic.StartsWith("shift") || opCode.mnemonic.StartsWith("ignore"))
                    continue;

                // Create the bytes
                byte[] bytes;
                byte[] suffixBytes = null;
                if (opIsSuffix)
                {
                    bytes = prefixBytes;
                    suffixBytes = new byte[] { (byte)i };
                }
                else
                {
                    bytes = new byte[prefixBytes.Length + 1];
                    Array.Copy(prefixBytes, bytes, prefixBytes.Length);
                    bytes[prefixBytes.Length] = (byte)i;
                }

                // Create the instruction
                AddInstruction(new InstructionDefinition()
                {
                    bytes = bytes,
                    suffixBytes = suffixBytes,
                    opCode = opCode,
                    mnemonic = opCode.mnemonic,
                });


                if (opCode.altMnemonic != null)
                {
                    AddInstruction(new InstructionDefinition()
                    {
                        bytes = bytes,
                        suffixBytes = suffixBytes,
                        opCode = opCode,
                        mnemonic = opCode.altMnemonic,
                    });
                }
            }
        }


        public static void DumpAll()
        {
            foreach (var kv in _opMap.OrderBy(x => x.Value.mnemonic))
            {
                //Console.WriteLine(kv.Key);

                // Is it a group?
                var group = kv.Value as InstructionGroup;
                if (group != null)
                {
                    Console.WriteLine($"<group>          {group.mnemonic}");
                    foreach (var d in group.Definitions)
                    {
                        d.Dump();
                    }
                }

                // Is it a definition?
                var def = kv.Value as InstructionDefinition;
                if (def != null)
                {
                    def.Dump();
                }
            }
        }


        #region Instruction Names
        static HashSet<string> _instructionNames;
        public static bool IsValidInstructionName(string name)
        {
            return _instructionNames.Contains(name);
        }
        #endregion

        #region Register Names
        static string[] _registerNames = new string[]
        {
            "AF", "AF'", "I",
            "A", "B", "C", "D", "E", "H", "L",
            "DE", "HL", "SP",
            "IX", "IY", "IXH", "IXL", "IYH", "IYL",
        };
        static HashSet<string> _registerNameMap = new HashSet<string>(_registerNames, StringComparer.InvariantCultureIgnoreCase);
        public static bool IsValidRegister(string name)
        {
            return _registerNameMap.Contains(name);
        }
        #endregion

        #region Condition Flag Names
        static string[] _conditionFlags = new string[]
        {
            "Z", "NZ", "C", "NC", "PE", "P", "PO",
        };
        static HashSet<string> _conditionFlagMap = new HashSet<string>(_conditionFlags, StringComparer.InvariantCultureIgnoreCase);
        public static bool IsConditionFlag(string name)
        {
            return _conditionFlagMap.Contains(name);
        }
        #endregion



    }
}
