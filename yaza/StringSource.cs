using System;
using System.Collections.Generic;
using System.Text;

namespace yaza
{
    public class StringSource
    {
        // Construct a string source from a string
        public StringSource(string str, string displayName = null, string location = null)
        {
            // Remove BOM
            if (str.Length > 0 && (str[0] == 0xFFFE || str[0] == 0xFEFF))
                str = str.Substring(1);

            _str = str;
            _pos = 0;
            _startPos = 0;
            _stopPos = str.Length;
            _displayName = displayName;
            _location = location;

            // Skip BOM if exists
            if (!Skip((char)0xFFFE))
                Skip((char)0xFEFF);
        }

        // Construct a string source from a string
        public StringSource(string str, int startPos, int length, string displayName = null, string location = null)
        {
            _str = str;
            _pos = startPos;
            _startPos = startPos;
            _stopPos = startPos + length;
            _displayName = displayName;
            _location = location;
        }

        public string SourceText
        {
            get { return _str.Substring(_startPos, _stopPos - _startPos); }
        }

        public StringSource CreateEmbeddedSource(int from, int length)
        {
            return new StringSource(_str, from, length, _displayName, _location);
        }

        public SourcePosition CreatePosition(int position)
        {
            return new SourcePosition(this, position);
        }

        public SourcePosition CapturePosition()
        {
            return new SourcePosition(this, _pos);
        }

        string _str;
        int _pos;
        int _startPos;
        int _stopPos;
        string _displayName;
        string _location;

        public string DisplayName => _displayName;
        public string Location => _location;

        // Have we reached the end of the file?
        public bool EOF => _pos >= _stopPos;

        // The current character
        public char Current => _pos < _stopPos ? _str[_pos] : '\0';

        // The current position
        public int Position
        {
            get => _pos;
            set
            {
                System.Diagnostics.Debug.Assert(value >= _startPos && value <= _stopPos);
                _pos = value;
            }
        }

        // The remaining text (handy for watching in debugger)
        public string Remaining => _str.Substring(_pos);

        // The character at offset from current
        public char CharAt(int offset)
        {
            var pos = _pos + offset;
            return pos < _stopPos ? _str[pos] : '\0';
        }

        // Move by n places
        public void Move(int delta)
        {
            _pos += delta;
            if (_pos < 0)
                _pos = 0;
            if (_pos > _stopPos)
                _pos = _stopPos;
        }

        // Move to the next character (if available)
        public void Next()
        {
            if (_pos < _stopPos)
                _pos++;
        }

        // Move to the previous character
        public void Previous()
        {
            if (_pos > 0)
                _pos--;
        }

        public string SkipRemaining()
        {
            var str = Remaining;
            _pos = _stopPos;
            return str;
        }

        public bool EOL
        {
            get
            {
                if (_pos >= _stopPos)
                    return true;

                return _str[_pos] == '\r' || _str[_pos] == '\n';
            }
        }

        public bool SkipToEOL()
        {
            // Skip to end of line
            int start = _pos;
            while (!EOF && _str[_pos] != '\r' && _str[_pos] != '\n')
                _pos++;
            return _pos > start;
        }

        public bool SkipEOL()
        {
            int oldPos = _pos;
            if (_pos < _stopPos && _str[_pos] == '\r')
                _pos++;
            if (_pos < _stopPos && _str[_pos] == '\n')
                _pos++;
            return _pos > oldPos;
        }

        public bool SkipToNextLine()
        {
            int start = _pos;
            SkipToEOL();
            SkipEOL();
            return _pos > start;
        }

        // Skip whitespace
        public bool SkipLinespace()
        {
            if (_pos >= _stopPos)
                return false;
            if (!IsLineSpace(_str[_pos]))
                return false;

            _pos++;
            while (_pos < _stopPos && IsLineSpace(_str[_pos]))
                _pos++;

            return true;

        }

        bool IsLineSpace(char ch)
        {
            return ch == ' ' || ch == '\t';
        }

        // Skip whitespace
        public bool SkipWhitespace()
        {
            if (_pos >= _stopPos)
                return false;
            if (!char.IsWhiteSpace(_str[_pos]))
                return false;

            _pos++;
            while (_pos < _stopPos && char.IsWhiteSpace(_str[_pos]))
                _pos++;

            return true;
        }

        // Check if the current position in the string matches a substring
        public bool DoesMatch(string str)
        {
            if (_pos + str.Length > _stopPos)
                return false;

            for (int i = 0; i < str.Length; i++)
            {
                if (_str[_pos + i] != str[i])
                    return false;
            }

            return true;
        }

        // Check if the current position in the string matches a substring (case insensitive)
        public bool DoesMatchI(string str)
        {
            if (_pos + str.Length > _stopPos)
                return false;

            for (int i = 0; i < str.Length; i++)
            {
                if (char.ToLowerInvariant(_str[_pos + i]) != char.ToLowerInvariant(str[i]))
                    return false;
            }

            return true;
        }

        // Skip forward until a particular string is matched
        public bool SkipUntil(string str)
        {
            while (_pos < _stopPos)
            {
                if (DoesMatch(str))
                    return true;
                _pos++;
            }
            return false;
        }

        // Skip forward until a particular string is matched (case insensitive)
        public bool SkipUntilI(string str)
        {
            while (_pos < _stopPos)
            {
                if (DoesMatchI(str))
                    return true;
                _pos++;
            }
            return false;
        }

        // Skip characters matching predicate
        public bool Skip(Func<char, bool> predicate)
        {
            if (_pos >= _stopPos || !predicate(_str[_pos]))
                return false;

            _pos++;
            while (_pos < _stopPos && predicate(_str[_pos]))
                _pos++;
            return true;
        }

        // Skip the specified character
        public bool Skip(char ch)
        {
            if (_pos >= _stopPos)
                return false;

            if (_str[_pos] != ch)
                return false;

            _pos++;
            return true;
        }

        // Skip the specified character
        public bool SkipI(char ch)
        {
            if (_pos >= _stopPos)
                return false;

            if (char.ToUpperInvariant(_str[_pos]) != char.ToUpperInvariant(ch))
                return false;

            _pos++;
            return true;
        }

        // Skip a string (case sensitive)
        public bool Skip(string str)
        {
            if (DoesMatch(str))
            {
                _pos += str.Length;
                return true;
            }
            return false;
        }

        // Skip a string (case insensitive)
        public bool SkipI(string str)
        {
            if (DoesMatchI(str))
            {
                _pos += str.Length;
                return true;
            }
            return false;
        }

        // Extract text from the specified position to the current position
        public string Extract(int fromPosition)
        {
            return _str.Substring(fromPosition, _pos - fromPosition);
        }

        public string Extract(int fromPosition, int toPosition)
        {
            return _str.Substring(fromPosition, toPosition - fromPosition);
        }

        // Skip characters matching predicate and return the matched characters
        public string SkipAndExtract(Func<char, bool> predicate)
        {
            int pos = _pos;
            if (!Skip(predicate))
                return null;
            return Extract(pos);
        }

        LineNumbers _lineNumbers;
        public LineNumbers LineNumbers
        {
            get
            {
                if (_lineNumbers == null)
                    _lineNumbers = new LineNumbers(_str);
                return _lineNumbers;
            }
        }

        StringBuilder _sb = new StringBuilder();
        public string Process(Func<char, StringBuilder, bool> callback)
        {
            _sb.Length = 0;
            while (_pos < _stopPos && callback(_str[_pos], _sb))
            {
                _pos++;
            }
            return _sb.ToString();
        }


    }


    // Represents a position within a StringSource
    // with helpers to map back to the source itself and the line
    // number and character offset
    public class SourcePosition
    {
        public SourcePosition(StringSource source, int pos)
        {
            Source = source;
            Position = pos;
        }

        public StringSource Source;
        public int Position;
        public int _lineNumber = -1;
        public int _charPosition = -1;
        public int LineNumber
        {
            get
            {
                if (_lineNumber < 0)
                {
                    Source.LineNumbers.FromFileOffset(Position, out _lineNumber, out _charPosition);
                }
                return _lineNumber;
            }
        }

        public int CharacterPosition
        {
            get
            {
                if (_lineNumber < 0)
                {
                    Source.LineNumbers.FromFileOffset(Position, out _lineNumber, out _charPosition);
                }
                return _charPosition;
            }
        }
    }

    static partial class Utils
    {
        public static string Describe(this SourcePosition pos)
        {
            if (pos == null)
                return "<unknown>";
            else
                return $"{pos.Source.DisplayName}({pos.LineNumber + 1},{pos.CharacterPosition + 1})";
        }

        public static string AstDesc(this SourcePosition pos)
        {
            if (pos == null)
                return "(no file location)";
            else
                return $"({pos.Source.DisplayName} {pos.LineNumber + 1},{pos.CharacterPosition + 1})";
        }
    }
}
