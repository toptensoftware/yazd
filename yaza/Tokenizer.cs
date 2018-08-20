using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    enum Token
    {
        EOF,
        EOL,

        Identifier,
        Number,
        String,

        Comma,
        Colon,
        OpenRound,
        CloseRound,

        Plus,
        Minus,
        Multiply,
        Divide,
        Modulus,

        Assign,

        Question,

        LogicalAnd,
        LogicalOr,
        LogicalNot,

        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseComplement,

        Shl,
        Shr,

        LT,
        GT,
        LE,
        GE,
        NE,
        EQ,
    }

    class Tokenizer
    {
        public Tokenizer(StringSource source)
        {
            _source = source;
            Next();
        }

        StringSource _source;

        Token _token;
        SourcePosition _tokenPosition;
        string _string;
        int _number;

        public Token Token => _token;
        public SourcePosition TokenPosition => _tokenPosition;
        public string TokenString => _string;
        public int TokenNumber => _number;

        public void Next()
        {
            _token = GetNextToken();
            return;
        }

        public StringSource Source => _source;

        public bool IsIdentifier(string str)
        {
            return _token == Token.Identifier && string.Compare(str, _string, true) == 0;
        }

        public bool TrySkipIdentifier(string str)
        {
            if (IsIdentifier(str))
            {
                Next();
                return true;
            }

            return false;
        }

        public bool TrySkipToken(Token token)
        {
            if (_token == token)
            {
                Next();
                return true;
            }
            return false;
        }

        Token GetNextToken()
        {
            if (_source.EOF)
                return Token.EOF;

            // Skip any linespace
            _source.SkipLinespace();

            // Capture the position of the current token
            _tokenPosition = _source.CapturePosition();

            // $nnnn hex number 
            // (need to do this before IsIdentifier which also acceepts '$')
            if (_source.Current == '$' && IsHexDigit(_source.CharAt(1)))
            {
                _source.Next();
                _number = Convert.ToInt32(_source.SkipAndExtract(IsHexDigit), 16);
                return Token.Number;
            }

            // Identifier?
            if (IsIdentifierLeadChar(_source.Current))
            {
                _string = _source.SkipAndExtract(IsIdentifierChar);

                // Special handling for AF' register
                if (_string.ToUpperInvariant() == "AF" && _source.Skip('\''))
                    _string += "'";

                return Token.Identifier;
            }

            // Number?
            if (IsDigit(_source.Current))
            {
                _number = ParseNumber();
                return Token.Number;
            }

            // Hex prefix
            if (_source.SkipI("&h"))
            {
                _number = Convert.ToInt32(_source.SkipAndExtract(IsHexDigit), 16);
                return Token.Number;
            }

            // Binary prefix
            if (_source.SkipI("&b"))
            {
                _number = Convert.ToInt32(_source.SkipAndExtract(IsBinaryDigit), 2);
                return Token.Number;
            }

            // Other characters
            switch (_source.Current)
            {
                case ';':
                    // Comment
                    _source.SkipToNextLine();
                    return Token.EOL;

                case ',':
                    _source.Next();
                    return Token.Comma;

                case ':':
                    _source.Next();
                    return Token.Colon;

                case '(':
                    _source.Next();
                    return Token.OpenRound;

                case ')':
                    _source.Next();
                    return Token.CloseRound;

                case '+':
                    _source.Next();
                    return Token.Plus;

                case '-':
                    _source.Next();
                    return Token.Minus;

                case '*':
                    _source.Next();
                    return Token.Multiply;

                case '/':
                    _source.Next();
                    return Token.Divide;

                case '%':
                    _source.Next();
                    return Token.Modulus;

                case '&':
                    _source.Next();
                    if (_source.Current == '&')
                    {
                        _source.Next();
                        return Token.LogicalAnd;
                    }
                    return Token.BitwiseAnd;

                case '|':
                    _source.Next();
                    if (_source.Current == '|')
                    {
                        _source.Next();
                        return Token.LogicalOr;
                    }
                    return Token.BitwiseOr;

                case '!':
                    _source.Next();
                    if (_source.Skip('='))
                        return Token.NE;
                    return Token.LogicalNot;

                case '^':
                    _source.Next();
                    return Token.BitwiseXor;

                case '~':
                    _source.Next();
                    return Token.BitwiseComplement;

                case '<':
                    _source.Next();
                    if (_source.Skip('<'))
                        return Token.Shl;
                    if (_source.Skip('='))
                        return Token.LE;
                    return Token.LT;

                case '>':
                    _source.Next();
                    if (_source.Skip('>'))
                        return Token.Shr;
                    if (_source.Skip('='))
                        return Token.GE;
                    return Token.GT;

                case '=':
                    _source.Next();
                    if (_source.Skip('='))
                        return Token.EQ;
                    return Token.Assign;

                case '?':
                    _source.Next();
                    return Token.Question;

                case '\r':
                case '\n':
                    _source.SkipEOL();
                    return Token.EOL;

                case '\'':
                case '\"':
                    _string = SkipString();
                    return Token.String;
            }

            throw new InvalidDataException(string.Format("Unexpected character in input: '{0}'", _source.Current));
        }

        StringBuilder _sb = new StringBuilder();

        string SkipString()
        {
            // Skip the delimiter
            var delim = _source.Current;
            _source.Next();

            // Reset working buffer
            _sb.Length = 0;

            // process characters
            while (!_source.EOF)
            {
                // Escape sequence?
                if (_source.Current == '\\')
                {
                    _source.Next();
                    var escape = _source.Current;
                    switch (escape)
                    {
                        case '\"': _sb.Append('\"'); break;
                        case '\\': _sb.Append('\\'); break;
                        case '/': _sb.Append('/'); break;
                        case 'b': _sb.Append('\b'); break;
                        case 'f': _sb.Append('\f'); break;
                        case 'n': _sb.Append('\n'); break;
                        case 'r': _sb.Append('\r'); break;
                        case 't': _sb.Append('\t'); break;
                        case 'u':
                            var sbHex = new StringBuilder();
                            for (int i = 0; i < 4; i++)
                            {
                                _source.Next();
                                sbHex.Append(_source.Current);
                            }
                            _sb.Append((char)Convert.ToUInt16(sbHex.ToString(), 16));
                            break;

                        default:
                            throw new InvalidDataException(string.Format("Invalid escape sequence in string literal: '\\{0}'", _source.Current));
                    }
                }
                else if (_source.Current == delim)
                {
                    // End of string
                    _source.Next();
                    return _sb.ToString();
                }
                else
                {
                    // Other character
                    _sb.Append(_source.Current);
                }

                // Next
                _source.Next();
            }

            throw new InvalidDataException("Unterminated string literatl");
        }

        int ParseNumber()
        {
            // Hex number
            if (_source.SkipI("0x"))
            {
                var hexNumber = _source.SkipAndExtract(IsHexDigit);
                return Convert.ToInt32(hexNumber, 16);
            }

            // Hex number
            if (_source.SkipI("0b"))
            {
                var hexNumber = _source.SkipAndExtract(IsBinaryDigit);
                return Convert.ToInt32(hexNumber, 2);
            }

            // Hex, decimal, or binary number
            var number = _source.SkipAndExtract(IsHexDigit);

            // Hex suffix?
            if (_source.SkipI('h'))
            {
                return Convert.ToInt32(number, 16);
            }

            // Octal?
            if (number[0] == '0')
                return Convert.ToInt32(number, 8);

            // Assume decimal
            return Convert.ToInt32(number);
        }

        bool IsDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        bool IsHexDigit(char ch)
        {
            return (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F') || (ch >= '0' && ch <= '9');
        }

        bool IsBinaryDigit(char ch)
        {
            return (ch >= '0' && ch <= '1');
        }

        bool IsOctalDigit(char ch)
        {
            return (ch >= '0' && ch <= '7');
        }

        bool IsIdentifierLeadChar(char ch)
        {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_' || ch == '$';
        }

        bool IsIdentifierChar(char ch)
        {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <='9') || ch == '_' || ch == '$';
        }
    }
}
