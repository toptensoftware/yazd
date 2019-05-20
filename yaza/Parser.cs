using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public class Parser
    {
        public Parser()
        {
        }

        Tokenizer _tokenizer;

        public List<string> IncludePaths { get; set; }

        public AstContainer Parse(string displayName, string location)
        {
            var filetext = System.IO.File.ReadAllText(location);
            var source = new StringSource(filetext + "\n", displayName, location);
            return Parse(source);
        }

        public ExprNode ParseExpression(string expr)
        {
            _tokenizer = new Tokenizer(new StringSource(expr));
            var exprNode = ParseExpression();
            _tokenizer.CheckToken(Token.EOF);
            return exprNode;
        }

        public AstContainer Parse(StringSource source)
        {
            try
            {
                // Create tokenizer
                _tokenizer = new Tokenizer(source);

                // Create AST container
                var container = new AstContainer(source.DisplayName);
                container.SourcePosition = _tokenizer.Source.CreatePosition(0);

                // Parse all elements
                ParseIntoContainer(container);

                // Should be at EOF?
                _tokenizer.CheckToken(Token.EOF);

                // Return the container
                return container;
            }
            catch
            {
                _tokenizer = null;
                throw;
            }
        }

        Parser OuterParser
        {
            get;
            set;
        }

        bool IsParsing(string filename)
        {
            if (_tokenizer.Source.Location == filename)
                return true;
            if (OuterParser != null)
                return OuterParser.IsParsing(filename);
            return false;
        }

        private void ParseIntoContainer(AstContainer container)
        {
            while (_tokenizer.Token != Token.EOF)
            {
                try
                {
                    var pos = _tokenizer.TokenPosition;

                    // Check for container terminators
                    if (_tokenizer.Token == Token.Identifier)
                    {
                        switch (_tokenizer.TokenString.ToUpperInvariant())
                        {
                            case "ENDIF":
                            case "ELSE":
                            case "ELSEIF":
                            case "ENDP":
                            case "ENDM":
                                return;
                        }
                    }

                    // Parse an element
                    var elem = ParseAstElement();

                    // Anything returned?
                    if (elem != null)
                    {
                        elem.SourcePosition = pos;
                        container.AddElement(elem);
                    }

                    // Unless it's a label, we should hit EOL after each element
                    if (!(elem is AstLabel))
                    {
                        _tokenizer.SkipToken(Token.EOL);
                    }
                }
                catch (CodeException x)
                {
                    // Log error
                    Log.Error(x.Position, x.Message);

                    // Skip to next line
                    while (_tokenizer.Token != Token.EOF && _tokenizer.Token != Token.EOL)
                    {
                        try
                        {
                            _tokenizer.Next();
                        }
                        catch (CodeException)
                        {
                            // Ignore other parse errors on this line
                        }
                    }
                }
            }
        }

        AstElement ParseAstElement()
        {
            // EOF?
            if (_tokenizer.Token == Token.EOF)
                return null;

            // Blank line?
            if (_tokenizer.Token == Token.EOL)
                return null;

            // ORG Element
            if (_tokenizer.TrySkipIdentifier("ORG"))
            {
                return new AstOrgElement(ParseExpression());
            }

            // SEEK Element
            if (_tokenizer.TrySkipIdentifier("SEEK"))
            {
                return new AstSeekElement(ParseExpression());
            }

            // END Element
            if (_tokenizer.TrySkipIdentifier("END"))
            {
                while (_tokenizer.Token != Token.EOF)
                    _tokenizer.Next();
                return null;
            }

            // Include?
            if (_tokenizer.TrySkipIdentifier("INCLUDE"))
            {
                // Load the include file
                string includeFile = ParseIncludePath();

                // Check for recursive inclusion of the same file
                if (IsParsing(includeFile))
                {
                    throw new CodeException($"error: recursive include file {_tokenizer.TokenRaw}", _tokenizer.TokenPosition);
                }

                string includeText;
                try
                {
                    includeText = System.IO.File.ReadAllText(includeFile);
                }
                catch (Exception x)
                {
                    throw new CodeException($"error: include file '{_tokenizer.TokenRaw}' - {x.Message}", _tokenizer.TokenPosition);
                }

                // Parse it
                var p = new Parser();
                p.OuterParser = this;
                var content = p.Parse(new StringSource(includeText + "\n", System.IO.Path.GetFileName(includeFile), includeFile));

                // Skip the filename
                _tokenizer.Next();

                // Return element
                return new AstInclude(includeFile, content);
            }

            // IncBin?
            if (_tokenizer.TrySkipIdentifier("INCBIN"))
            {
                // Load the include file
                string includeFile = ParseIncludePath(); 
                byte[] includeBytes;
                try
                {
                    includeBytes = System.IO.File.ReadAllBytes(includeFile);
                }
                catch (Exception x)
                {
                    throw new CodeException($"error loading incbin file '{includeFile}' - {x.Message}", _tokenizer.TokenPosition);
                }

                // Skip the filename
                _tokenizer.Next();

                // Return element
                return new AstIncBin(includeFile, includeBytes);
            }

            // DB?
            /*
            if (_tokenizer.TrySkipIdentifier("DB") || _tokenizer.TrySkipIdentifier("DEFB") 
                || _tokenizer.TrySkipIdentifier("DM") || _tokenizer.TrySkipIdentifier("DEFM"))
            {
                var elem = new AstDbElement();
                ParseDxExpressions(elem);
                return elem;
            }

            // DW?
            if (_tokenizer.TrySkipIdentifier("DW") || _tokenizer.TrySkipIdentifier("DEFW"))
            {
                var elem = new AstDwElement();
                ParseDxExpressions(elem);
                return elem;
            }
            */

            // DS?
            if (_tokenizer.TrySkipIdentifier("DS") || _tokenizer.TrySkipIdentifier("DEFS"))
            {
                var elem = new AstDsElement(ParseExpression());
                if (_tokenizer.TrySkipToken(Token.Comma))
                {
                    elem.ValueExpression = ParseExpression();
                }
                return elem;
            }

            // IF Block
            if (_tokenizer.TrySkipIdentifier("IF"))
            {
                return ParseConditional();
            }

            // PROC?
            if (_tokenizer.TrySkipIdentifier("PROC"))
            {
                _tokenizer.SkipToken(Token.EOL);

                var proc = new AstProc();
                ParseIntoContainer(proc);

                _tokenizer.SkipIdentifier("ENDP");

                return proc;
            }
            
            // RADIX
            if (_tokenizer.IsIdentifier("RADIX"))
            {
                var saveRadix = _tokenizer.DefaultRadix;
                try
                {
                    _tokenizer.DefaultRadix = 10;
                    _tokenizer.Next();
                    _tokenizer.CheckToken(Token.Number);

                    switch (_tokenizer.TokenNumber)
                    {
                        case 2:
                        case 8:
                        case 10:
                        case 16:
                            break;

                        default:
                            throw new CodeException("Invalid radix - must be 2, 8, 10, or 16", _tokenizer.TokenPosition);
                    }

                    _tokenizer.DefaultRadix = (int)_tokenizer.TokenNumber;
                    _tokenizer.Next();
                    return null;
                }
                catch
                {
                    _tokenizer.DefaultRadix = saveRadix;
                    throw;
                }
            }

            // Error
            if (_tokenizer.TrySkipIdentifier("ERROR"))
            {
                _tokenizer.CheckToken(Token.String);
                var message = _tokenizer.TokenString;
                _tokenizer.Next();

                return new AstErrorWarning(message, false);
            }

            // Warning
            if (_tokenizer.TrySkipIdentifier("WARNING"))
            {
                _tokenizer.CheckToken(Token.String);
                var message = _tokenizer.TokenString;
                _tokenizer.Next();

                return new AstErrorWarning(message, true);
            }

            // DEFBITS?
            if (_tokenizer.TrySkipIdentifier("DEFBITS"))
            {
                // Get the character
                _tokenizer.CheckToken(Token.String);
                var character = _tokenizer.TokenString;
                _tokenizer.Next();

                // Skip the comma
                _tokenizer.SkipToken(Token.Comma);

                if (_tokenizer.Token == Token.String)
                {
                    // Get the bit pattern
                    _tokenizer.CheckToken(Token.String);
                    var bitPattern = _tokenizer.TokenString;
                    _tokenizer.Next();
                    return new AstDefBits(character, bitPattern);
                }
                else
                {
                    var bitWidth = ParseExpression();
                    _tokenizer.SkipToken(Token.Comma);
                    var value = ParseExpression();
                    return new AstDefBits(character, value, bitWidth);
                }

            }

            // BITMAP
            if (_tokenizer.TrySkipIdentifier("BITMAP"))
            {
                // Parse width and height
                var width = ParseExpression();
                _tokenizer.SkipToken(Token.Comma);
                var height = ParseExpression();

                // Bit order spec?
                bool msbFirst = true;
                if (_tokenizer.TrySkipToken(Token.Comma))
                {
                    if (_tokenizer.TrySkipIdentifier("msb"))
                        msbFirst = true;
                    else if (_tokenizer.TrySkipIdentifier("lsb"))
                        msbFirst = false;
                    else
                        throw new CodeException("Expected 'MSB' or 'LSB'", _tokenizer.TokenPosition);
                }

                // Create bitmap ast element
                var bitmap = new AstBitmap(width, height,msbFirst);

                // Move to next line
                _tokenizer.SkipToken(Token.EOL);

                // Consume all strings, one per line
                while (_tokenizer.Token == Token.String)
                {
                    bitmap.AddString(_tokenizer.TokenString);
                    _tokenizer.Next();
                    _tokenizer.SkipToken(Token.EOL);
                    continue;
                }

                // Skip the end dilimeter
                bitmap.EndPosition = _tokenizer.TokenPosition;
                _tokenizer.SkipIdentifier("ENDB");
                return bitmap;
            }

            if (_tokenizer.Token == Token.Identifier)
            {
                // Remember the name
                var pos = _tokenizer.TokenPosition;
                var name = _tokenizer.TokenString;
                _tokenizer.Next();
                string[] paramNames = null;

                // Parameterized EQU?
                if (_tokenizer.Token == Token.OpenRound && !IsReservedWord(name))
                {
                    paramNames = ParseParameterNames();
                }

                // Followed by colon?
                bool haveColon = false;
                if (_tokenizer.TrySkipToken(Token.Colon))
                {
                    haveColon = true;
                }

                // EQU?
                if (_tokenizer.TrySkipIdentifier("EQU"))
                {
                    return new AstEquate(name, ParseOperandExpression(), pos)
                    {
                        ParameterNames = paramNames,
                    };
                }

                // MACRO?
                if (_tokenizer.TrySkipIdentifier("MACRO"))
                {
                    _tokenizer.SkipToken(Token.EOL);

                    var macro = new AstMacroDefinition(name, paramNames);
                    ParseIntoContainer(macro);

                    _tokenizer.SkipIdentifier("ENDM");

                    return macro;
                }

                // STRUCT?
                if (_tokenizer.TrySkipIdentifier("STRUCT"))
                {
                    var structDef = new AstStructDefinition(name);

                    // Process field definitions
                    while (_tokenizer.Token != Token.EOF)
                    {
                        // Skip blank lines
                        if (_tokenizer.TrySkipToken(Token.EOL))
                            continue;

                        // End of struct
                        if (_tokenizer.TrySkipIdentifier("ENDS"))
                            break;

                        // Capture the field name (or could be type name)
                        var fieldDefPos = _tokenizer.TokenPosition;
                        var fieldName = (string)null;

                        if (!_tokenizer.TrySkipToken(Token.Colon))
                        {
                            _tokenizer.CheckToken(Token.Identifier);
                            fieldName = _tokenizer.TokenString;

                            // Next token
                            _tokenizer.Next();
                        }

                        // Must be an identifier (for the type name)
                        _tokenizer.CheckToken(Token.Identifier);
                        var fieldType = _tokenizer.TokenString;
                        _tokenizer.Next();

                        // Add the field definition
                        structDef.AddField(new AstFieldDefinition(fieldDefPos, fieldName, fieldType, ParseExpressionList()));

                        _tokenizer.SkipToken(Token.EOL);
                    }

                    _tokenizer.SkipToken(Token.EOL);
                    return structDef;
                }

                // Nothing from here on expected parameters
                if (paramNames != null)
                    throw new CodeException("Unexpected parameter list in label", pos);

                // Was it a label?
                if (haveColon)
                {
                    if (IsReservedWord(name))
                        throw new CodeException($"Unexpected colon after reserved word '{name}'", pos);

                    return new AstLabel(name, pos);
                }

                // Is it an instruction?
                if (InstructionSet.IsValidInstructionName(name))
                {
                    return ParseInstruction(pos, name);
                }

                // Must be a macro invocation or a data declaration
                return ParseMacroInvocationOrDataDeclaration(pos, name);
            }

            // What?
            throw _tokenizer.Unexpected();
        }

        ExprNode ParseExpressionList()
        {
            var pos = _tokenizer.TokenPosition;
            var node = ParseExpression();
            if (_tokenizer.TrySkipToken(Token.Comma))
            {
                var concat = new ExprNodeConcat(pos);
                concat.AddElement(node);

                while (_tokenizer.Token != Token.EOL && _tokenizer.Token != Token.CloseRound)
                {
                    concat.AddElement(ParseExpression());

                    if (_tokenizer.TrySkipToken(Token.Comma))
                        continue;
                    else
                        break;
                }

                node = concat;
            }

            return node;
        }

        AstElement ParseConditional()
        {
            var ifBlock = new AstConditional();
            try
            {
                ifBlock.Condition = ParseExpression();
            }
            catch (CodeException x)
            {
                Log.Error(x);
                while (_tokenizer.Token != Token.EOF && _tokenizer.Token != Token.EOL)
                    _tokenizer.Next();
            }
            ifBlock.TrueBlock = new AstContainer("true block");
            ParseIntoContainer((AstContainer)ifBlock.TrueBlock);

            if (_tokenizer.TrySkipIdentifier("ELSE"))
            {
                ifBlock.FalseBlock = new AstContainer("false block");
                ParseIntoContainer((AstContainer)ifBlock.FalseBlock);
            }
            else if (_tokenizer.TrySkipIdentifier("ELSEIF"))
            {
                ifBlock.FalseBlock = ParseConditional();
                return ifBlock;
            }   

            _tokenizer.SkipIdentifier("ENDIF");
            return ifBlock;
        }

        string[] ParseParameterNames()
        {
            _tokenizer.SkipToken(Token.OpenRound);

            var names = new List<string>();
            while (true)
            {
                // Get parameter name
                _tokenizer.CheckToken(Token.Identifier);
                if (IsReservedWord(_tokenizer.TokenString))
                {
                    throw new CodeException($"Illegal use of reserved word as a name: '{_tokenizer.TokenString}'", _tokenizer.TokenPosition);
                }

                // Add to list
                names.Add(_tokenizer.TokenString);

                _tokenizer.Next();

                // End of list
                if (_tokenizer.TrySkipToken(Token.CloseRound))
                    return names.ToArray();

                // Comma
                _tokenizer.SkipToken(Token.Comma);
            }
        }

        string ParseIncludePath()
        {
            _tokenizer.CheckToken(Token.String, "for filename");

            // Look up relative to this file first
            try
            {
                var thisFileDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(_tokenizer.Source.Location));
                var includeFile = System.IO.Path.Combine(thisFileDir, _tokenizer.TokenString);
                if (System.IO.File.Exists(includeFile))
                    return includeFile;
            }
            catch
            {
            }

            // Search include paths
            var parser = this;
            while (parser != null)
            {
                if (parser.IncludePaths != null)
                {
                    foreach (var p in parser.IncludePaths)
                    {
                        try
                        {
                            var includeFile = System.IO.Path.Combine(p, _tokenizer.TokenString);
                            if (System.IO.File.Exists(includeFile))
                                return includeFile;
                        }
                        catch { }
                    }
                }

                // Try outer parser
                parser = parser.OuterParser;
            }

            // Assume current directory
            return _tokenizer.TokenString;
        }

        /*
        void ParseDxExpressions(AstDxElement elem)
        {
            while (true)
            {
                if (_tokenizer.Token == Token.String)
                {
                    if (elem is AstDbElement)
                    {
                        foreach (var b in Encoding.UTF8.GetBytes(_tokenizer.TokenString))
                        {
                            elem.AddValue(new ExprNodeLiteral(_tokenizer.TokenPosition, b));
                        }
                    }
                    else
                    {
                        foreach (var ch in _tokenizer.TokenString)
                        {
                            elem.AddValue(new ExprNodeLiteral(_tokenizer.TokenPosition, ch));
                        }
                    }
                    _tokenizer.Next();
                }
                else
                {
                    elem.AddValue(ParseExpression());
                }
                if (!_tokenizer.TrySkipToken(Token.Comma))
                    return;
            }
        }
        */

        AstInstruction ParseInstruction(SourcePosition pos, string name)
        {
            var instruction = new AstInstruction(pos, name);

            if (_tokenizer.Token == Token.EOL || _tokenizer.Token == Token.EOF)
                return instruction;

            while (true)
            {
                instruction.AddOperand(ParseOperandExpression());
                if (!_tokenizer.TrySkipToken(Token.Comma))
                    return instruction;
            }
        }

        AstMacroInvocationOrDataDeclaration ParseMacroInvocationOrDataDeclaration(SourcePosition pos, string name)
        {
            var macroInvocation = new AstMacroInvocationOrDataDeclaration(pos, name);

            if (_tokenizer.Token == Token.EOL || _tokenizer.Token == Token.EOF)
                return macroInvocation;

            while (true)
            {
                macroInvocation.AddOperand(ParseOperandExpression());
                if (!_tokenizer.TrySkipToken(Token.Comma))
                    return macroInvocation;
            }
        }

        ExprNode[] ParseArgumentList()
        {
            _tokenizer.SkipToken(Token.OpenRound);

            var args = new List<ExprNode>();
            while (true)
            {
                args.Add(ParseExpression());
                if (_tokenizer.TrySkipToken(Token.CloseRound))
                    return args.ToArray();
                _tokenizer.SkipToken(Token.Comma);
            }
        }

        ExprNode ParseOrderedStructData()
        {
            var array = new ExprNodeOrderedStructData(_tokenizer.TokenPosition);
            _tokenizer.SkipToken(Token.OpenSquare);

            _tokenizer.SkipWhitespace();

            while (_tokenizer.Token != Token.EOF && _tokenizer.Token != Token.CloseSquare)
            {
                array.AddElement(ParseExpression());
                if (_tokenizer.TrySkipToken(Token.Comma))
                {
                    _tokenizer.SkipWhitespace();
                    continue;
                }

                break;
            }

            _tokenizer.SkipWhitespace();
            _tokenizer.SkipToken(Token.CloseSquare);

            return array;
        }

        ExprNode ParseNamedStructData()
        {
            var map = new ExprNodeNamedStructData(_tokenizer.TokenPosition);
            _tokenizer.SkipToken(Token.OpenBrace);

            _tokenizer.SkipWhitespace();

            while (_tokenizer.Token != Token.EOF && _tokenizer.Token != Token.CloseBrace)
            {
                _tokenizer.SkipWhitespace();

                // Get the token name
                _tokenizer.CheckToken(Token.Identifier);
                var name = _tokenizer.TokenString;
                if (IsReservedWord(name))
                    throw new CodeException($"Illegal use of reserved word '{name}'", _tokenizer.TokenPosition);
                if (map.ContainsEntry(name))
                    throw new CodeException($"Duplicate key: '{name}'", _tokenizer.TokenPosition);
                _tokenizer.Next();

                // Skip the colon
                _tokenizer.SkipToken(Token.Colon);

                // Parse the value
                var value = ParseExpression();

                // Add it
                map.AddEntry(name, value);

                // Another value?
                if (_tokenizer.TrySkipToken(Token.Comma))
                {
                    _tokenizer.SkipWhitespace();
                    continue;
                }

                break;
            }

            _tokenizer.SkipWhitespace();
            _tokenizer.SkipToken(Token.CloseBrace);

            return map;
        }


        ExprNode ParseLeaf()
        {
            var pos = _tokenizer.TokenPosition;

            // Number literal?
            if (_tokenizer.Token == Token.Number)
            {
                var node = new ExprNodeNumberLiteral(pos, _tokenizer.TokenNumber);
                _tokenizer.Next();
                return node;
            }

            // String literal?
            if (_tokenizer.Token == Token.String)
            {
                var str = _tokenizer.TokenString;
                _tokenizer.Next();
                return new ExprNodeStringLiteral(pos, str);
            }

            // Defined operator?
            if (_tokenizer.TrySkipIdentifier("defined"))
            {
                _tokenizer.SkipToken(Token.OpenRound);
                _tokenizer.CheckToken(Token.Identifier);
                var node = new ExprNodeIsDefined(pos, _tokenizer.TokenString);
                _tokenizer.Next();
                _tokenizer.SkipToken(Token.CloseRound);
                return node;
            }

            // Sizeof operator?
            if (_tokenizer.TrySkipIdentifier("sizeof"))
            {
                _tokenizer.SkipToken(Token.OpenRound);
                var node = new ExprNodeSizeOf(pos, ParseExpression());
                _tokenizer.SkipToken(Token.CloseRound);
                return node;
            }

            // Identifier
            if (_tokenizer.Token == Token.Identifier)
            {
                // Special identifier
                if (InstructionSet.IsConditionFlag(_tokenizer.TokenString) ||
                    InstructionSet.IsValidRegister(_tokenizer.TokenString))
                {
                    var node = new ExprNodeRegisterOrFlag(pos, _tokenizer.TokenString);
                    _tokenizer.Next();
                    return node;
                }
                else
                {
                    var node = new ExprNodeIdentifier(pos, _tokenizer.TokenString);
                    _tokenizer.Next();

                    if (_tokenizer.Token == Token.Period)
                    {
                        ExprNode retNode = node;
                        while (_tokenizer.TrySkipToken(Token.Period))
                        {
                            _tokenizer.CheckToken(Token.Identifier);
                            retNode = new ExprNodeMember(_tokenizer.TokenPosition, _tokenizer.TokenString, retNode);
                            _tokenizer.Next();
                        }
                        return retNode;
                    }

                    if (_tokenizer.Token == Token.OpenRound)
                    {
                        node.Arguments = ParseArgumentList();
                    }

                    return node;
                }
            }

            // Parens?
            if (_tokenizer.TrySkipToken(Token.OpenRound))
            {
                var node = ParseExpressionList();
                _tokenizer.SkipToken(Token.CloseRound);
                return node;
            }

            // Array?
            if (_tokenizer.Token == Token.OpenSquare)
                return ParseOrderedStructData();

            // Map?
            if (_tokenizer.Token == Token.OpenBrace)
                return ParseNamedStructData();

            throw new CodeException($"syntax error in expression: {Tokenizer.DescribeToken(_tokenizer.Token, _tokenizer.TokenRaw)}", _tokenizer.TokenPosition);
        }

        ExprNode ParseUnary()
        {
            var pos = _tokenizer.TokenPosition;
            if (_tokenizer.TrySkipToken(Token.BitwiseComplement))
            {
                return new ExprNodeUnary(pos)
                {
                    OpName = "~",
                    Operator = ExprNodeUnary.OpBitwiseComplement,
                    RHS = ParseUnary(),
                };
            }

            if (_tokenizer.TrySkipToken(Token.LogicalNot))
            {
                return new ExprNodeUnary(pos)
                {
                    OpName = "!",
                    Operator = ExprNodeUnary.OpLogicalNot,
                    RHS = ParseUnary(),
                };
            }

            if (_tokenizer.TrySkipToken(Token.Minus))
            {
                return new ExprNodeUnary(pos)
                {
                    OpName = "-",
                    Operator = ExprNodeUnary.OpNegate,
                    RHS = ParseUnary(),
                };
            }

            if (_tokenizer.TrySkipToken(Token.Plus))
            {
                return ParseUnary();
            }
            

            // Parse leaf node
            return ParseLeaf();
        }

        ExprNode ParseMultiply()
        {
            var LHS = ParseUnary();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.Multiply))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseUnary(),
                        OpName = "*",
                        Operator = ExprNodeBinary.OpMul,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Divide))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseUnary(),
                        OpName = "/",
                        Operator = ExprNodeBinary.OpDiv,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Modulus))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseUnary(),
                        OpName = "%",
                        Operator = ExprNodeBinary.OpMod,
                    };
                }
                else
                    return LHS;
            }
        }

        ExprNode ParseAdd()
        {
            var LHS = ParseMultiply();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.Plus))
                {
                    LHS = new ExprNodeAdd(pos)
                    {
                        LHS = LHS,
                        RHS = ParseMultiply(),
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Minus))
                {
                    LHS = new ExprNodeAdd(pos)
                    {
                        LHS = LHS,
                        RHS = new ExprNodeUnary(pos)
                        {
                            RHS = ParseMultiply(),
                            OpName = "-",
                            Operator = ExprNodeUnary.OpNegate
                        }
                    };
                }
                else
                    return LHS;
            }
        }

        // Shift
        ExprNode ParseShift()
        {
            var LHS = ParseAdd();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.Shl))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseAdd(),
                        OpName = "<<",
                        Operator = ExprNodeBinary.OpShl,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Shr))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseAdd(),
                        OpName = ">>",
                        Operator = ExprNodeBinary.OpShr,
                    };
                }
                else
                    return LHS;
            }
        }

        ExprNode ParseDup()
        {
            var expr = ParseShift();

            var pos = _tokenizer.TokenPosition;
            if (_tokenizer.TrySkipIdentifier("DUP"))
            {
                return new ExprNodeDup(pos, expr, ParseTernery());
            }

            return expr;
        }

        // Relational
        ExprNode ParseRelational()
        {
            var LHS = ParseDup();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.LE))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseDup(),
                        OpName = "<=",
                        Operator = ExprNodeBinary.OpLE,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.GE))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseDup(),
                        OpName = ">=",
                        Operator = ExprNodeBinary.OpGE,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.GT))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseDup(),
                        OpName = ">",
                        Operator = ExprNodeBinary.OpGT,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.LT))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseDup(),
                        OpName = "<",
                        Operator = ExprNodeBinary.OpLT,
                    };
                }
                else
                    return LHS;
            }
        }

        // Equality
        ExprNode ParseEquality()
        {
            var LHS = ParseRelational();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.EQ))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseRelational(),
                        OpName = "==",
                        Operator = ExprNodeBinary.OpEQ,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.NE))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseRelational(),
                        OpName = "!=",
                        Operator = ExprNodeBinary.OpNE,
                    };
                }
                else
                    return LHS;
            }
        }

        // Bitwise AND
        ExprNode ParseBitwiseAnd()
        {
            var LHS = ParseEquality();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.BitwiseAnd))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseEquality(),
                        OpName = "&",
                        Operator = ExprNodeBinary.OpBitwiseAnd,
                    };
                }
                else
                    return LHS;
            }
        }


        // Bitwise XOR
        ExprNode ParseBitwiseXor()
        {
            var LHS = ParseBitwiseAnd();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.BitwiseXor))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseBitwiseAnd(),
                        OpName = "^",
                        Operator = ExprNodeBinary.OpBitwiseXor,
                    };
                }
                else
                    return LHS;
            }
        }


        // Bitwise OR
        ExprNode ParseBitwiseOr()
        {
            var LHS = ParseBitwiseXor();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.BitwiseOr))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseBitwiseXor(),
                        OpName = "|",
                        Operator = ExprNodeBinary.OpBitwiseOr,
                    };
                }
                else
                    return LHS;
            }
        }

        // Logical AND
        ExprNode ParseLogicalAnd()
        {
            var LHS = ParseBitwiseOr();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.LogicalAnd))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseBitwiseOr(),
                        OpName = "&&",
                        Operator = ExprNodeBinary.OpLogicalAnd,
                    };
                }
                else
                    return LHS;
            }
        }

        // Logical OR
        ExprNode ParseLogicalOr()
        {
            var LHS = ParseLogicalAnd();

            while (true)
            {
                var pos = _tokenizer.TokenPosition;
                if (_tokenizer.TrySkipToken(Token.LogicalOr))
                {
                    LHS = new ExprNodeBinary(pos)
                    {
                        LHS = LHS,
                        RHS = ParseLogicalAnd(),
                        OpName = "||",
                        Operator = ExprNodeBinary.OpLogicalOr,
                    };
                }
                else
                    return LHS;
            }
        }

        // Top level expression (except deref and z80 undocumented subops)
        ExprNode ParseTernery()
        {
            // Uninitialized data initializer?
            var pos = _tokenizer.TokenPosition;
            if (_tokenizer.TrySkipToken(Token.Question))
                return new ExprNodeUninitialized(pos);

            // Parse the condition part
            var condition = ParseLogicalOr();

            // Is it a conditional
            pos = _tokenizer.TokenPosition;
            if (_tokenizer.TrySkipToken(Token.Question))
            {
                var trueNode = ParseExpression();

                _tokenizer.SkipToken(Token.Colon);

                var falseNode = ParseExpression();

                return new ExprNodeTernery(pos)
                {
                    Condition = condition,
                    TrueValue = trueNode,
                    FalseValue = falseNode,
                };
            }

            return condition;
        }

        ExprNode ParseExpression()
        {
            var expr = ParseTernery();

            var pos = _tokenizer.TokenPosition;
            if (_tokenizer.TrySkipIdentifier("DUP"))
            {
                return new ExprNodeDup(pos, expr, ParseTernery());
            }

            return expr;
        }

        ExprNode ParseOperandExpression()
        {
            // Save position
            var pos = _tokenizer.TokenPosition;

            // Deref
            if (_tokenizer.TrySkipToken(Token.OpenRound))
            {
                var node = new ExprNodeDeref(pos);
                node.Pointer = ParseExpression();

                _tokenizer.SkipToken(Token.CloseRound);

                return node;
            }

            // Is it a sub op?
            if (_tokenizer.Token == Token.Identifier && InstructionSet.IsValidSubOpName(_tokenizer.TokenString))
            {
                var subOp = new ExprNodeSubOp(_tokenizer.TokenPosition, _tokenizer.TokenString);
                _tokenizer.Next();
                subOp.RHS = ParseOperandExpression();
                return subOp;
            }

            // Normal expression
            return ParseExpression();
        }

        public static bool IsReservedWord(string str)
        {
            if (str == null)
                return false;

            if (InstructionSet.IsConditionFlag(str) ||
                InstructionSet.IsValidInstructionName(str) ||
                InstructionSet.IsValidRegister(str))
                return true;

            switch (str.ToUpperInvariant())
            {
                case "ORG":
                case "SEEK":
                case "END":
                case "INCLUDE":
                case "INCBIN":
                case "EQU":
                case "DB":
                case "DEFB":
                case "DEFM":
                case "DW":
                case "DEFW":
                case "DS":
                case "DEFS":
                case "IF":
                case "ENDIF":
                case "ELSE":
                case "ELSEIF":
                case "PROC":
                case "ENDP":
                case "MACRO":
                case "ENDM":
                case "DEFBITS":
                case "BITMAP":
                case "ENDB":
                case "ERROR":
                case "WARNING":
                case "RADIX":
                case "STRUCT":
                case "ENDS":
                case "BYTE":
                case "WORD":
                    return true;
            }

            return false;
        }
    }
}
