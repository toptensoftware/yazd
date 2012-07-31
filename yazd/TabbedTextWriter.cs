using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace yazd
{
	class TabbedTextWriter  : TextWriter
	{
		public TabbedTextWriter(TextWriter Target)
		{
			_target = Target;
			_pos = 0;
		}

		TextWriter _target;
		int _pos;

		public int[] TabStops;

		// Write character
		public override void Write(char value)
		{
			Write(value.ToString());
		}

		// Write character buffer
		public override void Write(char[] buffer, int index, int count)
		{
			Write(new String(buffer, index, count));
		}

		// Write (overridden to do automatic indentation)
		public override void Write(string val)
		{
			foreach (var ch in val)
			{
				if (ch == '\t')
				{
					int tabStop = -1;
					for (int i = 0; i < TabStops.Length; i++)
					{
						if (_pos < TabStops[i])
						{
							tabStop = TabStops[i];
							break;
						}
					}

					if (tabStop == -1)
					{
						tabStop = (int)((_pos + 5) / 4) * 4;
					}

					_target.Write(new String(' ', tabStop - _pos));
					_pos = tabStop;

				}
				else
				{
					_target.Write(ch);

					if (ch == '\r' || ch == '\n')
						_pos = 0;
					else
						_pos++;
				}
			}
		}

		// Required by TextWriter
		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}

	}
}
