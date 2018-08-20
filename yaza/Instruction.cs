using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yazd;

namespace yaza
{
    public class Instruction
    {
        public string mnemonic;
        public OpCode opCode;
        public byte[] bytes;
        public byte[] suffixBytes;

        // Given a mnemonic pattern, find the associated instruction
        public static Instruction Find(string mnemonic)
        {
            Instruction op;
            if (!_opMap.TryGetValue(mnemonic, out op))
                return null;
            return op;
        }

        public string InstructionName
        {
            get
            {
                var space = mnemonic.IndexOf(' ');
                if (space < 0)
                    return mnemonic;
                else
                    return mnemonic.Substring(0, space);
            }
        }


        public void Dump()
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                Console.Write("{0:X2} ", bytes[i]);
            }

            int length = bytes.Length;
            int operandCount = 0;
            if (mnemonic.IndexOf('@') >= 0)
            {
                Console.Write("@@ @@ ");
                length += 2;
                operandCount++;
            }
            if (mnemonic.IndexOf('$') >= 0)
            {
                Console.Write("$$ ");
                length++;
                operandCount++;
            }
            if (mnemonic.IndexOf("#") >= 0)
            {
                Console.Write("## ");
                length++;
                operandCount++;
            }
            if (mnemonic.IndexOf('%') >= 0)
            {
                Console.Write("%% ");
                length++;
                operandCount++;
            }

            if (suffixBytes != null)
            {
                for (int i = 0; i < suffixBytes.Length; i++)
                {
                    Console.Write("{0:X2} ", suffixBytes[i]);
                    length++;
                }
            }

            while (length < 5)
            {
                Console.Write("   ");
                length++;
            }

            if (operandCount > 1)
            {
                Console.Write("? ");
            }
            else
            {
                Console.Write("  ");
            }

            Console.WriteLine(mnemonic);
        }

        static Dictionary<string, Instruction> _opMap = new Dictionary<string, Instruction>();

        static string KeyOfMnemonic(string mnemonic)
        {
            return mnemonic
                .Replace('%', '?')
                .Replace('@', '?')
                .Replace('#', '?')
                .Replace('$', '?');
        }

        static void AddInstruction(Instruction instruction)
        {
            string key = KeyOfMnemonic(instruction.mnemonic);

            Instruction existing;
            if (_opMap.TryGetValue(key, out existing))
            {
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
                AddInstruction(new Instruction()
                {
                    bytes = bytes,
                    suffixBytes = suffixBytes,
                    opCode = opCode,
                    mnemonic = opCode.mnemonic,
                });


                if (opCode.altMnemonic != null)
                {
                    AddInstruction(new Instruction()
                    {
                        bytes = bytes,
                        suffixBytes = suffixBytes,
                        opCode = opCode,
                        mnemonic = opCode.altMnemonic,
                    });
                }
            }
        }

        static HashSet<string> _instructionNames;

        public static bool IsValidInstructionName(string name)
        {
            return _instructionNames.Contains(name);
        }

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

        static string[] _conditionFlags = new string[]
        {
            "Z", "NZ", "C", "NC", "PE", "P", "PO", 
        };
        static HashSet<string> _conditionFlagMap = new HashSet<string>(_conditionFlags, StringComparer.InvariantCultureIgnoreCase);
        public static bool IsConditionFlag(string name)
        {
            return _conditionFlagMap.Contains(name);
        }


        static Instruction()
        {
            ProcessOpCodeTable(OpCodes.dasm_base, new byte[0]);
            ProcessOpCodeTable(OpCodes.dasm_ed, new byte[] { 0xED });
            ProcessOpCodeTable(OpCodes.dasm_cb, new byte[] { 0xCB });
            ProcessOpCodeTable(OpCodes.dasm_dd, new byte[] { 0xDD });
            ProcessOpCodeTable(OpCodes.dasm_fd, new byte[] { 0xFD });
            ProcessOpCodeTable(OpCodes.dasm_ddcb, new byte[] { 0xDD, 0xCB }, true);
            ProcessOpCodeTable(OpCodes.dasm_fdcb, new byte[] { 0xFD, 0xCB }, true);

            // Build a list of all instruction names
            _instructionNames = new HashSet<string>(_opMap.Values.Select(x=>x.InstructionName), StringComparer.InvariantCultureIgnoreCase);
        }

        public static void DumpAll()
        {
            foreach (var m in _opMap.Values.OrderBy(x => x.mnemonic))
            {
                m.Dump();
            }
        }
    }



}
