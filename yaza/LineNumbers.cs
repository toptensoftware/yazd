using System;
using System.Collections.Generic;
using System.Text;

namespace yaza
{
    public class LineNumbers
    {
        public LineNumbers(string str)
        {
            _totalLength = str.Length;
            // Build a list of the offsets of the start of all lines
            _lineOffsets.Add(0);
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\r' && i + 1 < str.Length && str[i + 1] == '\n')
                    i++;

                if (str[i] == '\n' || str[i] == '\r')
                {
                    _lineOffsets.Add(i + 1);
                }
            }
        }

        // Convert a file offset to a line number and character position
        public bool FromFileOffset(int fileOffset, out int lineNumber, out int charPosition)
        {
            // Look up line offset
            for (int i=1; i<_lineOffsets.Count; i++)
            {
                if (fileOffset < _lineOffsets[i])
                {
                    lineNumber = i - 1;
                    charPosition = fileOffset - _lineOffsets[i - 1];
                    return true;
                }
            }
            
            // Past the last line?
            lineNumber = _lineOffsets.Count - 1;
            charPosition = fileOffset - _lineOffsets[_lineOffsets.Count - 1];
            return false;
        }

        // Convert a line number and character position to a file offset
        public int ToFileOffset(int lineNumber, int characterPosition)
        {
            if (lineNumber > _lineOffsets.Count)
                return _totalLength;

            int offset = _lineOffsets[lineNumber] + characterPosition;
            if (offset > _totalLength)
                offset = _totalLength;
            return offset;
        }

        List<int> _lineOffsets = new List<int>();
        int _totalLength;
    }
}
