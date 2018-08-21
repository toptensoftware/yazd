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

        public AstContainer Parse(string displayName, string location)
        {
            var filetext = System.IO.File.ReadAllText(location);
            var source = new StringSource(filetext, displayName, location);
            return Parse(source);
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
                        _tokenizer.Next();
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
                var content = p.Parse(new StringSource(includeText, System.IO.Path.GetFileName(includeFile), includeFile));

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
            if (_tokenizer.TrySkipIdentifier("DB") || _tokenizer.TrySkipIdentifier("DEFB"))
            {
                var elem = new AstDbElement();
                ParseDxExpressions(elem);
                return elem;
            }

            // DW?
            if (_tokenizer.TrySkipIdentifier("DW") || _tokenizer.TrySkipIdentifier("DEFW") || _tokenizer.TrySkipIdentifier("DM") || _tokenizer.TrySkipIdentifier("DEFM"))
            {
                var elem = new AstDwElement();
                ParseDxExpressions(elem);
                return elem;
            }

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

            if (_tokenizer.Token == Token.Identifier)
            {
                // Remember the name
                var pos = _tokenizer.TokenPosition;
                var name = _tokenizer.TokenString;
                _tokenizer.Next();

                // Label or equate?
                if (_tokenizer.TrySkipToken(Token.Colon))
                {
                    // Equate?
                    if (_tokenizer.TrySkipIdentifier("EQU"))
                    {
                        return new AstEquate(name, ParseOperandExpression());
                    }

                    // Label
                    return new AstLabel(name, pos);
                }

                // Alternate syntax for EQU (no colon)
                if (_tokenizer.TrySkipIdentifier("EQU"))
                {
                    return new AstEquate(name, ParseOperandExpression());
                }

                // Must be an instruction
                if (InstructionSet.IsValidInstructionName(name))
                {
                    return ParseInstruction(pos, name);
                }

                throw new CodeException($"syntax error: '{name}'", pos);
            }

            // What?
            throw _tokenizer.Unexpected();
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

            // Assume current directory
            return _tokenizer.TokenString;
        }

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
                            elem.AddValue(new ExprNodeLiteral(b));
                        }
                    }
                    else
                    {
                        foreach (var ch in _tokenizer.TokenString)
                        {
                            elem.AddValue(new ExprNodeLiteral(ch));
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

        ExprNode ParseLeaf()
        {
            // Number literal?
            if (_tokenizer.Token == Token.Number)
            {
                var node = new ExprNodeLiteral(_tokenizer.TokenNumber);
                _tokenizer.Next();
                return node;
            }

            // Identifier
            if (_tokenizer.Token == Token.Identifier)
            {
                // Special identifier
                if (InstructionSet.IsConditionFlag(_tokenizer.TokenString) ||
                    InstructionSet.IsValidRegister(_tokenizer.TokenString))
                {
                    var node = new ExprNodeRegisterOrFlag(_tokenizer.TokenPosition, _tokenizer.TokenString);
                    _tokenizer.Next();
                    return node;
                }
                else
                {
                    var node = new ExprNodeIdentifier(_tokenizer.TokenPosition, _tokenizer.TokenString);
                    _tokenizer.Next();
                    return node;
                }
            }

            // Parens?
            if (_tokenizer.TrySkipToken(Token.OpenRound))
            {
                var node = ParseExpression();
                _tokenizer.SkipToken(Token.CloseRound);
                return node;
            }


            throw new CodeException($"syntax error in expression: '{Tokenizer.DescribeToken(_tokenizer.Token)}'", _tokenizer.TokenPosition);
        }

        ExprNode ParseUnary()
        {
            if (_tokenizer.TrySkipToken(Token.BitwiseComplement))
            {
                return new ExprNodeUnary()
                {
                    OpName = "~",
                    Operator = ExprNodeUnary.OpBitwiseComplement,
                    RHS = ParseUnary(),
                };
            }

            if (_tokenizer.TrySkipToken(Token.LogicalNot))
            {
                return new ExprNodeUnary()
                {
                    OpName = "!",
                    Operator = ExprNodeUnary.OpLogicalNot,
                    RHS = ParseUnary(),
                };
            }

            if (_tokenizer.TrySkipToken(Token.Minus))
            {
                return new ExprNodeUnary()
                {
                    OpName = "-",
                    Operator = ExprNodeUnary.OpNegate,
                    RHS = ParseUnary(),
                };
            }

            // Parse leaf node
            return ParseLeaf();
        }

        ExprNode ParseMultiply()
        {
            var LHS = ParseUnary();

            while (true)
            {
                if (_tokenizer.TrySkipToken(Token.Multiply))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseUnary(),
                        OpName = "*",
                        Operator = ExprNodeBinary.OpMul,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Divide))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseUnary(),
                        OpName = "/",
                        Operator = ExprNodeBinary.OpDiv,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Modulus))
                {
                    LHS = new ExprNodeBinary()
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

            // Add or subtract?
            if (_tokenizer.Token != Token.Plus && _tokenizer.Token != Token.Minus)
            {
                return LHS;
            }

            // Create add LTR chain
            var ltr = new ExprNodeAddLTR();
            ltr.AddNode(LHS);

            // Build chain of add nodes
            while (true)
            {
                if (_tokenizer.TrySkipToken(Token.Plus))
                {
                    ltr.AddNode(ParseMultiply());
                }
                else if (_tokenizer.TrySkipToken(Token.Minus))
                {
                    ltr.AddNode(new ExprNodeUnary() {
                        RHS = ParseMultiply(),
                        OpName = "-",
                        Operator = ExprNodeUnary.OpNegate
                    });
                }
                else
                    return ltr;
            }
        }

        // Shift
        ExprNode ParseShift()
        {
            var LHS = ParseAdd();

            while (true)
            {
                if (_tokenizer.TrySkipToken(Token.Shl))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseAdd(),
                        OpName = "<<",
                        Operator = ExprNodeBinary.OpShl,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.Shr))
                {
                    LHS = new ExprNodeBinary()
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

        // Relational
        ExprNode ParseRelational()
        {
            var LHS = ParseShift();

            while (true)
            {
                if (_tokenizer.TrySkipToken(Token.LE))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseShift(),
                        OpName = "<=",
                        Operator = ExprNodeBinary.OpLE,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.GE))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseShift(),
                        OpName = ">=",
                        Operator = ExprNodeBinary.OpGE,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.GT))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseShift(),
                        OpName = ">",
                        Operator = ExprNodeBinary.OpGT,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.LT))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseShift(),
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
                if (_tokenizer.TrySkipToken(Token.EQ))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseRelational(),
                        OpName = "==",
                        Operator = ExprNodeBinary.OpEQ,
                    };
                }
                else if (_tokenizer.TrySkipToken(Token.NE))
                {
                    LHS = new ExprNodeBinary()
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
                if (_tokenizer.TrySkipToken(Token.BitwiseAnd))
                {
                    LHS = new ExprNodeBinary()
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
                if (_tokenizer.TrySkipToken(Token.BitwiseXor))
                {
                    LHS = new ExprNodeBinary()
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
                if (_tokenizer.TrySkipToken(Token.BitwiseOr))
                {
                    LHS = new ExprNodeBinary()
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
                if (_tokenizer.TrySkipToken(Token.LogicalAnd))
                {
                    LHS = new ExprNodeBinary()
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
            var LHS = ParseBitwiseAnd();

            while (true)
            {
                if (_tokenizer.TrySkipToken(Token.LogicalOr))
                {
                    LHS = new ExprNodeBinary()
                    {
                        LHS = LHS,
                        RHS = ParseBitwiseAnd(),
                        OpName = "||",
                        Operator = ExprNodeBinary.OpLogicalOr,
                    };
                }
                else
                    return LHS;
            }
        }

        // Ternery
        ExprNode ParseExpression()
        {
            // Capture position
            var pos = _tokenizer.TokenPosition;

            // Parse the condition part
            var condition = ParseLogicalOr();

            // Is it a conditional
            if (_tokenizer.TrySkipToken(Token.Question))
            {
                var trueNode = ParseExpression();

                _tokenizer.SkipToken(Token.Colon);

                var falseNode = ParseExpression();

                return new ExprNodeTernery()
                {
                    SourcePosition = pos,
                    Condition = condition,
                    TrueValue = trueNode,
                    FalseValue = falseNode,
                };
            }

            if (condition.SourcePosition == null)
                condition.SourcePosition = pos;
            return condition;
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
                var subOp = new ExprNodeSubOp(_tokenizer.TokenString);
                _tokenizer.Next();
                subOp.RHS = ParseOperandExpression();
                return subOp;
            }

            // Normal expression
            return ParseExpression();
        }
    }
}
