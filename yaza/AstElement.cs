using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    // Base class for all AST elements
    public abstract class AstElement
    {
        public AstElement()
        {
        }

        // The container holding this element
        public AstContainer Container;

        public SourcePosition SourcePosition;

        // Find the close containing scope
        public AstScope ContainingScope
        {
            get
            {
                var temp = Container;
                while (temp != null)
                {
                    var scope = temp as AstScope;
                    if (scope != null)
                        return scope;
                    temp = temp.Container;
                }
                return null;
            }
        }

        public abstract void Dump(TextWriter w, int indent);

        public virtual void DefineSymbols(AstScope currentScope)
        {
        }

        public virtual void Layout(AstScope currentScope, LayoutContext ctx)
        {

        }

        public virtual void Generate(AstScope currentScope, GenerateContext ctx)
        {

        }
    }

    // Container for other AST elements
    public class AstContainer : AstElement
    {
        public AstContainer(string name)
        {
            _name = name;
        }

        string _name;

        public string Name => _name;

        public virtual void AddElement(AstElement element)
        {
            element.Container = this;
            _elements.Add(element);
        }

        protected List<AstElement> _elements = new List<AstElement>();

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- CONTAINER {Name} {SourcePosition.AstDesc()}");
            foreach (var e in _elements)
            {
                e.Dump(w, indent + 1);
            }
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            foreach (var e in _elements)
            {
                try
                {
                    e.DefineSymbols(currentScope);
                }
                catch (CodeException x)
                {
                    Log.Error(x);
                }
            }
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            foreach (var e in _elements)
            {
                try
                {
                    e.Layout(currentScope, ctx);
                }
                catch (CodeException x)
                {
                    Log.Error(x);
                }
            }
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            foreach (var e in _elements)
            {
                try
                {
                    e.Generate(currentScope, ctx);
                }
                catch (CodeException x)
                {
                    Log.Error(x);
                }
            }
        }

    }

    public class AstConditional : AstElement
    {
        public AstConditional()
        {
        }

        public ExprNode Condition;
        public AstElement TrueBlock;
        public AstElement FalseBlock;
        bool _isTrue;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- CONDITIONAL {SourcePosition.AstDesc()}");
            Condition.Dump(w, indent + 1);

            w.WriteLine($"{Utils.Indent(indent + 1)}- TRUE BLOCK");
            TrueBlock.Dump(w, indent + 2);

            if (FalseBlock != null)
            {
                w.WriteLine($"{Utils.Indent(indent + 1)}- FALSE BLOCK");
                FalseBlock.Dump(w, indent + 2);
            }
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            _isTrue = Condition.Evaluate(currentScope) != 0;

            if (_isTrue)
                TrueBlock.DefineSymbols(currentScope);
            else if (FalseBlock != null)
                FalseBlock.DefineSymbols(currentScope);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            if (_isTrue)
                TrueBlock.Layout(currentScope, ctx);
            else if (FalseBlock != null)
                FalseBlock.Layout(currentScope, ctx);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            if (_isTrue)
                TrueBlock.Generate(currentScope, ctx);
            else if (FalseBlock != null)
                FalseBlock.Generate(currentScope, ctx);
        }
    }


    // Like a container but contains symbol definitions
    public class AstScope : AstContainer
    {
        public AstScope(string name) : base(name)
        {
        }


        // Define a symbols value
        public void Define(string symbol, ISymbolValue value)
        {
            // Check not already defined in an outer scope
            var outerScope = ContainingScope;
            if (outerScope != null && outerScope.IsSymbolDefined(symbol))
            {
                throw new InvalidOperationException(string.Format("The symbol '{0}' is already defined in an outer scope", symbol));
            }

            // Check if already defined
            ISymbolValue existing;
            if (_symbols.TryGetValue(symbol, out existing))
            {
                throw new InvalidOperationException(string.Format("Duplicate symbol: '{0}'", symbol));
            }

            // Store it
            _symbols[symbol] = value;
        }

        // Check if a symbol is defined in this scope or any outer scope
        public bool IsSymbolDefined(string symbol)
        {
            return FindSymbol(symbol) != null;
        }

        // Find the definition of a symbol
        public ISymbolValue FindSymbol(string symbol)
        {
            ISymbolValue value;
            if (_symbols.TryGetValue(symbol, out value))
                return value;

            var outerScope = ContainingScope;
            if (outerScope == null)
                return null;

            return outerScope.FindSymbol(symbol);
        }

        // Dictionary of symbols in this scope
        Dictionary<string, ISymbolValue> _symbols = new Dictionary<string, ISymbolValue>(StringComparer.InvariantCultureIgnoreCase);

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- SCOPE {Name} {SourcePosition.AstDesc()}");
            foreach (var e in _elements)
            {
                e.Dump(w, indent + 1);
            }
        }

        public void DumpSymbols(TextWriter w)
        {
            w.WriteLine("Symbols:");
            foreach (var kv in _symbols)
            {
                var sym = kv.Value as ExprNode;
                if (sym != null && !(sym is ExprNodeParameterized))
                    w.WriteLine($"    {kv.Key,20}: 0x{sym.Evaluate(this):X4}");
            }
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            if (this is AstMacroDefinition)
                base.DefineSymbols(currentScope);
            else
                base.DefineSymbols(this);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            if (this is AstMacroDefinition)
                base.Layout(currentScope, ctx);
            else
                base.Layout(this, ctx);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            if (this is AstMacroDefinition)
                base.Generate(currentScope, ctx);
            else
                base.Generate(this, ctx);
        }

    }

    // "ORG" directive
    public class AstOrgElement : AstElement
    {
        public AstOrgElement(ExprNode expr)
        {
            _expr = expr;
        }

        ExprNode _expr;
        int _address;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- ORG {SourcePosition.AstDesc()}");
            _expr.Dump(w, indent + 1);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            ctx.SetOrg(_address = _expr.Evaluate(currentScope));
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            ctx.SetOrg(SourcePosition, _address);
        }
    }

    // "SEEK" directive
    public class AstSeekElement : AstElement
    {
        public AstSeekElement(ExprNode expr)
        {
            _expr = expr;
        }

        ExprNode _expr;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- SEEK {SourcePosition.AstDesc()}");
            _expr.Dump(w, indent + 1);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            ctx.Seek(_expr.Evaluate(currentScope));
        }
    }

    // "DB" or "DW" directive
    public abstract class AstDxElement : AstElement
    {
        public void AddValue(ExprNode value)
        {
            _values.Add(value);
        }

        protected List<ExprNode> _values = new List<ExprNode>();

        public override void Dump(TextWriter w, int indent)
        {
            foreach (var v in _values)
            {
                v.Dump(w, indent);
            }
        }
    }

    // "DB" directive
    public class AstDbElement : AstDxElement
    {
        public AstDbElement()
        {
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- DB {SourcePosition.AstDesc()}");
            base.Dump(w, indent + 1);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            ctx.ReserveBytes(_values.Count);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            foreach (var e in _values)
            {
                ctx.Emit8(e.SourcePosition, e.Evaluate(currentScope));
            }
        }
    }

    // "DW" directive
    public class AstDwElement : AstDxElement
    {
        public AstDwElement()
        {
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- DW {SourcePosition.AstDesc()}");
            base.Dump(w, indent + 1);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            ctx.ReserveBytes(_values.Count * 2);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            foreach (var e in _values)
            {
                ctx.Emit16(e.SourcePosition, e.Evaluate(currentScope));
            }
        }
    }

    // "DS" directive
    public class AstDsElement : AstElement
    {
        public AstDsElement(ExprNode bytesExpr)
        {
            _bytesExpression = bytesExpr;
        }

        ExprNode _bytesExpression;
        int _bytes;

        public ExprNode ValueExpression;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- DW {SourcePosition.AstDesc()}");
            _bytesExpression.Dump(w, indent + 1);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            ctx.ReserveBytes(_bytes = _bytesExpression.Evaluate(currentScope));
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);

            if (ctx.ListFile != null)
            {
                ctx.ListTo(SourcePosition);
                ctx.WriteListingText($"{ctx.ip:X4}: [{_bytes} bytes]");
                ctx.ListToInclusive(SourcePosition);
            }

            var bytes = new byte[_bytes];

            if (ValueExpression != null)
            {
                var value = ValueExpression.Evaluate(currentScope);

                // Check range (yes, sbyte and byte)
                if (value < sbyte.MinValue || value > byte.MaxValue)
                {
                    Log.Error(SourcePosition, $"value out of range: {value} (0x{value:X}) doesn't fit in 8-bits");
                }
                else
                {
                    var val = (byte)(value & 0xFF);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = val;
                    }
                }
            }

            ctx.EmitBytes(bytes, false);
        }
    }

    // Label
    public class AstLabel : AstElement
    {
        public AstLabel(string name, SourcePosition position)
        {
            _name = name;
            _value = new ExprNodeDeferredValue(name, position);
        }

        string _name;
        ExprNodeDeferredValue _value;

        public string Name => _name;
        public ExprNode Value => _value;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- LABEL '{_name}' {SourcePosition.AstDesc()}");
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            currentScope.Define(_name, _value);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            _value.Resolve(ctx.ip);
        }

    }

    // "EQU" directive
    public class AstEquate : AstElement
    {
        public AstEquate(string name, ExprNode value)
        {
            _name = name;
            _value = value;
        }

        string _name;
        ExprNode _value;

        public string Name => _name;
        public ExprNode Value => _value;

        public string[] ParameterNames;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- EQU '{_name}' {SourcePosition.AstDesc()}");

            if (ParameterNames != null)
            {
                w.WriteLine($"{Utils.Indent(indent + 1)}- PARAMETERS");
                foreach (var p in ParameterNames)
                {
                    w.WriteLine($"{Utils.Indent(indent + 2)}- '{p}'");
                }
            }

            _value.Dump(w, indent + 1);
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            if (ParameterNames == null)
            {
                currentScope.Define(_name, _value);
            }
            else
            {
                var value = new ExprNodeParameterized(ParameterNames, _value);
                currentScope.Define(_name + ExprNodeParameterized.MakeSuffix(ParameterNames.Length), value);
            }
        }
    }

    // Include directive
    public class AstInclude : AstElement
    {
        public AstInclude(string filename, AstContainer content)
        {
            _filename = filename;
            _content = content;
        }

        string _filename;
        AstContainer _content;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- INCLUDE '{_filename}' {SourcePosition.AstDesc()}");
            _content.Dump(w, indent + 1);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            _content.Layout(currentScope, ctx);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListToInclusive(SourcePosition);
            ctx.EnterSourceFile(_content.SourcePosition);
            _content.Generate(currentScope, ctx);
            ctx.LeaveSourceFile();
        }

    }

    // IncBin directive
    public class AstIncBin : AstElement
    {
        public AstIncBin(string filename, byte[] data)
        {
            _filename = filename;
            _data = data;
        }

        string _filename;
        byte[] _data;

        public override void Dump(TextWriter w, int indent)
        {
            w.Write($"{Utils.Indent(indent)}- INCBIN '{_filename}' {SourcePosition.AstDesc()}");

            for (int i = 0; i < _data.Length; i++)
            {
                if ((i % 16) == 0)
                    w.Write($"\n{Utils.Indent(indent + 1)}- ");
                w.Write("{0:X2} ", _data[i]);
            }

            if (_data.Length > 0)
                w.WriteLine();
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            ctx.ReserveBytes(_data.Length);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            if (ctx.ListFile != null)
            {
                ctx.ListTo(SourcePosition);
                ctx.WriteListingText($"{ctx.ip:X4}: [{_data.Length} bytes]");
                ctx.ListToInclusive(SourcePosition);
            }

            ctx.EmitBytes(_data, false);
        }
    }

    public class AstInstruction : AstElement
    {
        public AstInstruction(SourcePosition position, string mnemonic)
        {
            _position = position;
            _mnemonic = mnemonic;
        }

        public void AddOperand(ExprNode operand)
        {
            _operands.Add(operand);
        }

        SourcePosition _position;
        string _mnemonic;
        List<ExprNode> _operands = new List<ExprNode>();
        Instruction _instruction;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- {_mnemonic.ToUpperInvariant()} {SourcePosition.AstDesc()}");
            foreach (var o in _operands)
            {
                o.Dump(w, indent + 1);
            }
        }

        bool IsIndexRegister(string reg)
        {
            // Don't insert implicit +0 for (IX) and (IY) when JP instruction
            if (_mnemonic.ToUpper() == "JP")
                return false;

            reg = reg.ToUpperInvariant();
            return reg == "IX" || reg == "IY";
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            var sb = new StringBuilder();

            sb.Append(_mnemonic);

            for (int i = 0; i < _operands.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                else
                    sb.Append(" ");

                var o = _operands[i];

                var addressingMode = o.GetAddressingMode(currentScope);

                if ((addressingMode & AddressingMode.SubOp) != 0)
                {
                    sb.Append(o.GetSubOp());
                    sb.Append(" ");
                    addressingMode = addressingMode & ~AddressingMode.SubOp;
                }

                switch (addressingMode)
                {
                    case AddressingMode.Deref | AddressingMode.Immediate:
                        sb.Append($"(?)");
                        break;

                    case AddressingMode.Deref | AddressingMode.Register:
                        {
                            var reg = o.GetRegister(currentScope);
                            if (IsIndexRegister(reg))
                            {
                                sb.Append($"({reg}+?)");
                            }
                            else
                            {
                                sb.Append($"({reg})");
                            }
                        }
                        break;

                    case AddressingMode.Deref | AddressingMode.RegisterPlusImmediate:
                        sb.Append($"({o.GetRegister(currentScope)}+?)");
                        break;

                    case AddressingMode.Immediate:
                        sb.Append($"?");
                        break;
                    case AddressingMode.Register:
                        sb.Append($"{o.GetRegister(currentScope)}");
                        break;

                    case AddressingMode.RegisterPlusImmediate:
                        sb.Append($"{o.GetRegister(currentScope)}+?");
                        break;
                }
            }

            _instruction = InstructionSet.Find(sb.ToString());

            if (_instruction == null)
            {
                Log.Error(_position, $"invalid addressing mode: {sb.ToString()}");
            }
            else
            {
                ctx.ReserveBytes(_instruction.Length);
            }
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            ctx.EnterInstruction(this);
            try
            {
                List<int> immediateValues = null;
                for (int i = 0; i < _operands.Count; i++)
                {
                    var o = _operands[i];

                    var addressingMode = o.GetAddressingMode(currentScope);

                    if ((addressingMode & AddressingMode.Register) != 0 &&
                        (addressingMode & AddressingMode.Deref) != 0 &&
                        (addressingMode & AddressingMode.Immediate) == 0 &&
                        IsIndexRegister(o.GetRegister(currentScope)))
                    {
                        // Create the list if not already
                        if (immediateValues == null)
                            immediateValues = new List<int>();
                        immediateValues.Add(0);
                        continue;
                    }

                    if ((addressingMode & AddressingMode.Immediate) != 0)
                    {
                        // Create the list if not already
                        if (immediateValues == null)
                            immediateValues = new List<int>();

                        // Get the immediate value
                        immediateValues.Add(o.GetImmediateValue(currentScope));
                    }
                }

                // Generate the instruction
                _instruction.Generate(ctx, SourcePosition, immediateValues?.ToArray());
            }
            finally
            {
                ctx.LeaveInstruction();
            }
        }
    }

    public class AstProc : AstScope
    {
        public AstProc() : base("PROC")
        {
        }
    }

    public class AstMacroDefinition : AstScope, ISymbolValue
    {
        public AstMacroDefinition(string name, string[] parameters)
            : base("MACRO " + name)
        {
            _name = name;
            _parameters = parameters;
        }

        string _name;
        string[] _parameters;

        public override void DefineSymbols(AstScope currentScope)
        {
            if (_parameters == null)
                _parameters = new string[0];

            // Define the symbol
            currentScope.Define(_name + ExprNodeParameterized.MakeSuffix(_parameters.Length), this);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            // no-op
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            // no-op
        }

        public AstScope Resolve(AstScope currentScope, ExprNode[] arguments)
        {
            // Create scope
            var scope = new AstScope("MACRO INVOCATION");

            // Define it
            for (int i = 0; i < _parameters.Length; i++)
            {
                scope.Define(_parameters[i], arguments[i]);
            }

            // Setup outer scope
            scope.Container = currentScope;

            return scope;
        }

        public void DefineSymbolsResolved(AstScope resolvedScope)
        {
            base.DefineSymbols(resolvedScope);
        }

        public void LayoutResolved(AstScope resolvedScope, LayoutContext ctx)
        {
            base.Layout(resolvedScope, ctx);
        }

        public void GenerateResolved(AstScope resolvedScope, GenerateContext ctx)
        {
            base.Generate(resolvedScope, ctx);
        }
    }

    public class AstMacroInvocation : AstElement
    {
        public AstMacroInvocation(SourcePosition position, string macro)
        {
            _position = position;
            _macro = macro;
        }

        public void AddOperand(ExprNode operand)
        {
            _operands.Add(operand);
        }

        SourcePosition _position;
        string _macro;
        List<ExprNode> _operands = new List<ExprNode>();

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- {_macro} {SourcePosition.AstDesc()}");
            foreach (var o in _operands)
            {
                o.Dump(w, indent + 1);
            }
        }

        public override void DefineSymbols(AstScope currentScope)
        {
        }


        AstMacroDefinition _macroDefinition;
        AstScope _resolvedScope;
        void ResolveMacroDefinition(AstScope currentScope)
        {
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            // Find the macro definition
            _macroDefinition = currentScope.FindSymbol(_macro + ExprNodeParameterized.MakeSuffix(_operands.Count)) as AstMacroDefinition;
            if (_macroDefinition == null)
                throw new CodeException($"Unrecognized macro: '{_macro}' (with {_operands.Count} arguments)", SourcePosition);

            // Create resolved scope
            _resolvedScope = _macroDefinition.Resolve(currentScope, _operands.ToArray());

            // Define macro symbols
            _macroDefinition.DefineSymbolsResolved(_resolvedScope);

            // Layout 
            _macroDefinition.LayoutResolved(_resolvedScope, ctx);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            ctx.EnterMacro();
            try
            {
                _macroDefinition.GenerateResolved(_resolvedScope, ctx);
            }
            finally
            {
                ctx.LeaveMacro();
            }
        }
    }
}