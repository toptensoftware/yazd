using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yazd;

namespace yaza
{
    public abstract class Instruction
    {
        // The simplified version of the mnemonic (ie: with immediate arguments replaced by '?')
        public string mnemonic;

        // Final calculated length of the instruction in bytes
        public int Length;

        // Get just the instruction name from the mnemonic
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

        public abstract void Generate(GenerateContext ctx, SourcePosition sourcePosition, long[] immArgs);
    }

    // Represents and instruction group (ie: a set of instructions with variations on an immediate value)
    //  eg: SET 0,(HL), SET 1,(HL), SET 2,(HL) etc.. all form a group
    // Typically instruction group are the same instruction with a limited set of values implemented
    // as a bit pattern within the opcodes.  Rather than get into sub-byte bit handling, we just
    // fake the immediate parameters on top of the underlying instruction definitions
    public class InstructionGroup : Instruction
    {
        public InstructionGroup()
        {
        }

        // Add an instruction definition
        public void AddInstructionDefinition(int imm, InstructionDefinition definition)
        {
            // All variations must have the same length
            if (_immVariations.Count == 0)
            {
                Length = definition.Length;
            }
            else
            {
                System.Diagnostics.Debug.Assert(Length == definition.Length);
            }

            if (!_immVariations.ContainsKey(imm))
                _immVariations.Add(imm, definition);
        }

        // Find the definition for a specified immediate value
        public InstructionDefinition FindDefinition(int imm)
        {
            InstructionDefinition def;
            if (_immVariations.TryGetValue(imm, out def))
                return def;

            return null;
        }

        public IEnumerable<InstructionDefinition> Definitions
        {
            get
            {
                foreach (var k in _immVariations.OrderBy(x => x.Key))
                {
                    yield return k.Value;
                }
            }
        }

        public override void Generate(GenerateContext ctx, SourcePosition sourcePosition, long[] immArgs)
        {
            // Find the definition
            InstructionDefinition def;
            if (!_immVariations.TryGetValue((int)immArgs[0], out def))
            {
                throw new CodeException($"the immediate value 0x{immArgs[0]:X2} isn't a valid value for this instruction");
            }

            // Pass to the undelying definition (after removing the imm variation arg)
            def.Generate(ctx, sourcePosition, immArgs.Skip(1).ToArray());
        }

        // For instructions that contain hard coded immediate values eg: RST 0x68, SET 7,(HL) etc...
        // this is a dictionary of of the immediate values to a real final instruction
        Dictionary<int, InstructionDefinition> _immVariations = new Dictionary<int, InstructionDefinition>();
    }


    // Represents a concrete definition of an instruction
    public class InstructionDefinition : Instruction
    { 
        // The op code definition
        public OpCode opCode;

        // Instruction bytes (before the immediate values)
        public byte[] bytes;

        // Instruction suffix bytes (null if none, after the immediate values)
        public byte[] suffixBytes;


        // Prepare this instruction instance
        public void Prepare()
        {
            // Calculate the length of the instruction in bytes
            Length = bytes.Length;
            if (suffixBytes != null)
                Length += suffixBytes.Length;

            foreach (var ch in mnemonic)
            {
                switch (ch)
                {
                    case '@':
                        Length += 2;
                        break;

                    case '$':
                    case '%':
                    case '#':
                        Length++;
                        break;
                }
            }
        }

        // Prepare this instruction instance
        public override void Generate(GenerateContext ctx, SourcePosition sourcePosition, long[] immArgs)
        {
            var oldIp = ctx.ip;

            // Emit the bytes before the value
            ctx.EmitBytes(bytes, true);

            // Emit the immediate bytes
            int arg = 0;
            foreach (var ch in mnemonic)
            {
                switch (ch)
                {
                    case '@':
                        // 16-bit immediate
                        ctx.Emit16(sourcePosition, immArgs[arg++]);
                        break;

                    case '$':
                        ctx.Emit8(sourcePosition, immArgs[arg++]);
                        break;

                    case '%':
                        ctx.EmitRelOffset(sourcePosition, (int)immArgs[arg++]);
                        break;

                    case '#':
                        // 8 bit immediate
                        ctx.Emit8(sourcePosition, immArgs[arg++]);
                        break;
                }
            }


            // Emit the suffix bytes
            if (suffixBytes != null)
                ctx.EmitBytes(suffixBytes, true);

            // Sanity checks
            System.Diagnostics.Debug.Assert(arg == (immArgs == null ? 0 : immArgs.Length));
            System.Diagnostics.Debug.Assert(ctx.ip - oldIp == this.Length);
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
    }
}
