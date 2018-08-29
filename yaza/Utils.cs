using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yaza
{
	public static partial class Utils
    {
        public static string Indent(int indent)
        {
            return new string(' ', indent * 2);
        }

        public static string TypeName(object o)
        {
            if (o == null)
                return "null";
            var name = o.GetType().Name;
            if (name.StartsWith("ExprNode"))
                return name.Substring(8).ToLowerInvariant();
            return name;
        }

        public static byte PackByte(SourcePosition pos, object value)
        {
            if (value is long)
                return PackByte(pos, (long)value);
            Log.Error(pos, $"Can't convert {Utils.TypeName(value)} to byte");
            return 0xFF;
        }

        public static ushort PackWord(SourcePosition pos, object value)
        {
            if (value is long)
                return PackWord(pos, (long)value);
            Log.Error(pos, $"Can't convert {Utils.TypeName(value)} to word");
            return 0xFF;
        }

        public static byte PackByte(SourcePosition pos, long value)
        {
            // Check range (yes, sbyte and byte)
            if (value < sbyte.MinValue || value > byte.MaxValue)
            {
                Log.Error(pos, $"value out of range: {value} (0x{value:X}) doesn't fit in 8-bits");
                return 0xFF;
            }
            else
            {
                return (byte)(value & 0xFF);
            }
        }

        public static ushort PackWord(SourcePosition pos, long value)
        {
            // Check range (yes, short and ushort)
            if (value < short.MinValue || value > ushort.MaxValue)
            {
                Log.Error(pos, $"value out of range: {value} (0x{value:X}) doesn't fit in 16-bits");
                return 0xFFFF;
            }
            else
            {
                return (ushort)(value & 0xFFFF);
            }
        }

        public static ushort ParseUShort(string str)
		{
			try
			{
				if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
				{
					return Convert.ToUInt16(str.Substring(2), 16);
				}
				else
				{
					return ushort.Parse(str);
				}
			}
			catch (Exception)
			{
				throw new InvalidOperationException(string.Format("Invalid number: '{0}'", str));
			}
		}

		public static int[] ParseIntegers(string str, int Count)
		{
			var values = new List<int>();
			if (str != null)
			{
				foreach (var n in str.Split(','))
				{
					values.Add(int.Parse(n));
				}
			}

			if (Count != 0 && Count != values.Count)
			{
				throw new InvalidOperationException(string.Format("Invalid value - expected {0} comma separated values", Count));
			}


			return values.ToArray();
		}


		public static List<string> ParseCommandLine(string args)
		{
			var newargs = new List<string>();

			var temp = new StringBuilder();

			int i = 0;
			while (i < args.Length)
			{
				if (char.IsWhiteSpace(args[i]))
				{
					i++;
					continue;
				}

				bool bInQuotes = false;
				temp.Length = 0;
				while (i < args.Length && (!char.IsWhiteSpace(args[i]) || bInQuotes))
				{
					if (args[i] == '\"')
					{
						if (args[i + 1] == '\"')
						{
							temp.Append("\"");
							i++;
						}
						else
						{
							bInQuotes = !bInQuotes;
						}
					}
					else
					{
						temp.Append(args[i]);
					}

					i++;
				}

				if (temp.Length > 0)
				{
					newargs.Add(temp.ToString());
				}
			}

			return newargs;
		}

	}
}
