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

        public AstContainer Parse(StringSource source)
        {
            try
            {
                // Create tokenizer
                _tokenizer = new Tokenizer(source);

                // Create AST container
                var container = new AstContainer();

                while (_tokenizer.Token != Token.EOF)
                {
                    ParseIntoContainer(container);
                }

                return container;
            }
            catch
            {
                _tokenizer = null;
                throw;
            }
        }

        private void ParseIntoContainer(AstContainer container)
        {
            // Parse an element
            var elem = ParseAstElement();

            // Anything returned?
            if (elem != null)
                container.AddElement(elem);

            // If it's a label, add it to the current scope
            var label = elem as AstLabel;
            if (label != null)
            {
//                container.ContainingScope.Define(label.Name, label.Value);
                return;
            }

            // If it's an equate, add it to the current scope
            var equ = elem as AstEquate;
            if (equ != null)
            {
//                container.ContainingScope.Define(equ.Name, equ.Value);
            }

            // Must be eol (or eof) (unless it's a label)
            if (_tokenizer.Token != Token.EOF && _tokenizer.Token != Token.EOL)
            {
                throw new InvalidOperationException(string.Format("Unexpected tokens at end of line: {0}", _tokenizer.Token));
            }

            // Skip EOL
            _tokenizer.Next();
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

            // Include?
            if (_tokenizer.TrySkipIdentifier("INCLUDE"))
            {
                // Load the include file
                string includeFile = ParseIncludePath();
                string includeText;
                try
                {
                    includeText = System.IO.File.ReadAllText(includeFile);
                }
                catch (Exception x)
                {
                    throw new InvalidOperationException(string.Format("Failed to load include file '{0}' - {1}", includeFile, x.Message));
                }

                // Parse it
                var p = new Parser();
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
                    throw new InvalidOperationException(string.Format("Failed to load incbin file '{0}' - {1}", includeFile, x.Message));
                }

                // Skip the filename
                _tokenizer.Next();

                // Return element
                return new AstIncBin(includeFile, includeBytes);
            }

            // DB?
            if (_tokenizer.TrySkipIdentifier("DB"))
            {
                var elem = new AstDbElement();
                ParseDxExpressions(elem);
                return elem;
            }

            // DW?
            if (_tokenizer.TrySkipIdentifier("DW"))
            {
                var elem = new AstDwElement();
                ParseDxExpressions(elem);
                return elem;
            }

            // Remember the name
            var name = _tokenizer.TokenString;
            _tokenizer.Next();

            // Label or equate?
            if (_tokenizer.TrySkipToken(Token.Colon))
            {
                // Equate?
                if (_tokenizer.TrySkipIdentifier("EQU"))
                {
                    return new AstEquate(name, ParseDerefExpression());
                }

                // Label
                return new AstLabel(name);
            }

            // Alternate syntax for EQU (no colon)
            if (_tokenizer.TrySkipIdentifier("EQU"))
            {
                return new AstEquate(name, ParseDerefExpression());
            }

            // Must be an instruction
            if (Instruction.IsValidInstructionName(name))
            {
                return ParseInstruction(name);
            }

            // What?
            throw new InvalidOperationException(string.Format("Unexpected token: {0}", _tokenizer.Token));
        }

        string ParseIncludePath()
        {
            if (_tokenizer.Token != Token.String)
            {
                throw new InvalidOperationException("Expected string filename");
            }

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

        AstInstruction ParseInstruction(string name)
        {
            var instruction = new AstInstruction(name);

            if (_tokenizer.Token == Token.EOL || _tokenizer.Token == Token.EOF)
                return instruction;

            while (true)
            {
                instruction.AddOperand(ParseDerefExpression());
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
                if (Instruction.IsConditionFlag(_tokenizer.TokenString) ||
                    Instruction.IsValidRegister(_tokenizer.TokenString))
                {
                    var node = new ExprNodeRegisterOrFlag(_tokenizer.TokenString);
                    _tokenizer.Next();
                    return node;
                }
                else
                {
                    var node = new ExprNodeIdentifier(_tokenizer.TokenString);
                    _tokenizer.Next();
                    return node;
                }
            }

            // Parens?
            if (_tokenizer.TrySkipToken(Token.OpenRound))
            {
                var node = ParseExpression();
                if (_tokenizer.Token != Token.CloseRound)
                {
                    throw new InvalidOperationException("Missing close parens");
                }
                _tokenizer.Next();
                return node;
            }

            throw new InvalidOperationException(string.Format("Syntax error, unexpected token in expression: '{0}'", _tokenizer.Token));
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
            var condition = ParseLogicalOr();

            if (_tokenizer.TrySkipToken(Token.Question))
            {
                var trueNode = ParseExpression();

                if (!_tokenizer.TrySkipToken(Token.Colon))
                    throw new InvalidOperationException("Expected ':' in ternery expression");

                var falseNode = ParseExpression();

                return new ExprNodeTernery()
                {
                    Condition = condition,
                    TrueValue = trueNode,
                    FalseValue = falseNode,
                };
            }

            return condition;
        }

        ExprNode ParseDerefExpression()
        {
            if (!_tokenizer.TrySkipToken(Token.OpenRound))
                return ParseExpression();

            var node = new ExprNodeDeref();
            node.Pointer = ParseExpression();

            if (!_tokenizer.TrySkipToken(Token.CloseRound))
            {
                throw new InvalidOperationException("Syntax error: missing close paren");
            }

            return node;
        }
    }
}
