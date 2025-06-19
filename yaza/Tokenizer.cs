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
        Period,
        Colon,
        OpenRound,
        CloseRound,
        OpenSquare,
        CloseSquare,
        OpenBrace,
        CloseBrace,

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

        Unknown,
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
        string _string;
        long _number;
        int _tokenPos;

        public Token Token => _token;
        public SourcePosition TokenPosition => _source.CreatePosition(_tokenPos);
        public string TokenRaw => _source.Extract(_tokenPos);
        public string TokenString => _string;
        public long TokenNumber => _number;

        public void Next()
        {
            _token = GetNextToken();
            return;
        }

        public void SkipWhitespace()
        {
            while (_token == Token.EOL)
                Next();
        }

        public StringSource Source => _source;

        public bool IsIdentifier(string str)
        {
            return _token == Token.Identifier && string.Equals(str, _string, StringComparison.OrdinalIgnoreCase);
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

        public void SkipIdentifier(string str)
        {
            if (IsIdentifier(str))
            {
                Next();
                return;
            }

            throw new CodeException($"syntax error: expected '{str}', found '{TokenRaw}'", TokenPosition);
        }

        public int DefaultRadix = 10;


        public bool TrySkipToken(Token token)
        {
            if (_token == token)
            {
                Next();
                return true;
            }
            return false;
        }

        public void CheckToken(Token token, string suffix = null)
        {
            // If wanting EOL, EOF also allowed
            if (token == Token.EOL && _token == Token.EOF)
                return;

            if (_token != token)
            {
                if (suffix != null)
                    throw new CodeException($"syntax error: expected {DescribeToken(token)} {suffix}, found {DescribeToken(Token, TokenRaw)}", TokenPosition);
                else
                    throw new CodeException($"syntax error: expected {DescribeToken(token)}, found {DescribeToken(Token, TokenRaw)}", TokenPosition);
            }
        }

        public void SkipToken(Token token)
        {
            CheckToken(token);
            Next();
        }

        public CodeException Unexpected()
        {
            return new CodeException($"syntax error: unexpected: {DescribeToken(Token, TokenRaw)}", TokenPosition);
        }

        public CodeException Unexpected(string expected)
        {
            return new CodeException($"syntax error: {DescribeToken(Token, TokenRaw)}, expected {expected}", TokenPosition);
        }

        public static string DescribeToken(Token token, string raw = null)
        {
            switch (token)
            {
                case Token.EOF: return "end of file"; 
                case Token.EOL: return "end of line";
                case Token.Identifier: return "identifier";
                case Token.Number: return "number";
                case Token.String: return "string";
                case Token.Comma: return "','";
                case Token.Period: return "'.'";
                case Token.Colon: return "':'";
                case Token.OpenRound: return "'('";
                case Token.CloseRound: return "')'";
                case Token.OpenSquare: return "'['";
                case Token.CloseSquare: return "']'";
                case Token.OpenBrace: return "'{'";
                case Token.CloseBrace: return "'}'";
                case Token.Plus: return "'+'";
                case Token.Minus: return "'-'";
                case Token.Multiply: return "'*'";
                case Token.Divide: return "'/'";
                case Token.Modulus: return "'%'";
                case Token.Assign: return "'-'";
                case Token.Question: return "'?'";
                case Token.LogicalAnd: return "&&";
                case Token.LogicalOr: return "'||'";
                case Token.LogicalNot: return "'!'";
                case Token.BitwiseAnd: return "'%";
                case Token.BitwiseOr: return "|";
                case Token.BitwiseXor: return "^";
                case Token.BitwiseComplement: return "~";
                case Token.Shl: return "<<";
                case Token.Shr: return ">>";
                case Token.LT: return "<";
                case Token.GT: return ">";
                case Token.LE: return "<=";
                case Token.GE: return ">=";
                case Token.NE: return "!=";
                case Token.EQ: return "==";
            }

            if (raw != null)
                return $"unknown token: '{raw}'";
            else
                return "unknown token";
       }

        Token GetNextToken()
        {
            if (_source.EOF)
                return Token.EOF;

            // Skip any linespace
            _source.SkipLinespace();

            // Capture the position of the current token
            _tokenPos = _source.Position;

            // $nnnn hex number 
            // (need to do this before IsIdentifier which also acceepts '$')
            if (_source.Current == '$' && IsHexDigit(_source.CharAt(1)))
            {
                _source.Next();
                _number = Convert.ToInt64(_source.SkipAndExtract(IsHexDigit), 16);
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
                _number = Convert.ToInt64(_source.SkipAndExtract(IsHexDigit), 16);
                return Token.Number;
            }

            // Binary prefix
            if (_source.SkipI("&b"))
            {
                _number = Convert.ToInt64(_source.SkipAndExtract(IsBinaryDigit), 2);
                return Token.Number;
            }

            // Decimal prefix
            if (_source.SkipI("&n") || _source.SkipI("&d"))
            {
                _number = Convert.ToInt64(_source.SkipAndExtract(IsDigit), 10);
                return Token.Number;
            }

            // Octal prefix
            if (_source.SkipI("&o") || _source.SkipI("&t"))
            {
                _number = Convert.ToInt64(_source.SkipAndExtract(IsOctalDigit), 8);
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

                case '.':
                    _source.Next();
                    return Token.Period;

                case ':':
                    _source.Next();
                    return Token.Colon;

                case '(':
                    _source.Next();
                    return Token.OpenRound;

                case ')':
                    _source.Next();
                    return Token.CloseRound;

                case '[':
                    _source.Next();
                    return Token.OpenSquare;

                case ']':
                    _source.Next();
                    return Token.CloseSquare;

                case '{':
                    _source.Next();
                    return Token.OpenBrace;

                case '}':
                    _source.Next();
                    return Token.CloseBrace;

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

            _source.Next();
            return Token.Unknown;
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
            while (!_source.EOF && !_source.EOL)
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
                            Log.Warning(_source.CapturePosition(), "invalid escape in string literal");
                            _sb.Append("\\");
                            _sb.Append(escape);
                            break;
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

            Log.Warning(_source.CapturePosition(), "unterminated string literal");
            return _sb.ToString();
        }

        int ParseNumber()
        {
            // Hex number
            if (_source.SkipI("0x"))
            {
                var hexNumber = _source.SkipAndExtract(IsHexDigit);
                return Convert.ToInt32(hexNumber, 16);
            }

            // Decimal number
            if (_source.SkipI("0n"))
            {
                var hexNumber = _source.SkipAndExtract(IsHexDigit);
                return Convert.ToInt32(hexNumber, 10);
            }

            // Octal
            if (_source.SkipI("0t"))
            {
                var hexNumber = _source.SkipAndExtract(IsHexDigit);
                return Convert.ToInt32(hexNumber, 8);
            }

            // Hex, decimal, or binary number
            var number = _source.SkipAndExtract(IsHexDigit);

            try
            {
                // Hex suffix?
                if (_source.SkipI('h'))
                {
                    return Convert.ToInt32(number, 16);
                }

                // Octal suffix
                if (_source.SkipI('o') || _source.SkipI('q') || _source.SkipI('t'))
                {
                    return Convert.ToInt32(number, 8);
                }

                // Decimal suffix
                if (_source.SkipI('n'))
                {
                    return Convert.ToInt32(number, 10);
                }

                // Hex number
                if (number.StartsWith("0b"))
                {
                    return Convert.ToInt32(number.Substring(2), 2);
                }

                // Binary suffix
                if (number.EndsWith("d"))
                {
                    return Convert.ToInt32(number.Substring(0, number.Length - 1), 10);
                }

                // Binary suffix
                if (number.EndsWith("b"))
                {
                    return Convert.ToInt32(number.Substring(0, number.Length - 1), 2);
                }

                // default radix
                return Convert.ToInt32(number, DefaultRadix);
            }
            catch (Exception)
            {
                throw new CodeException($"Misformed number: '{number}'", TokenPosition);
            }
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
