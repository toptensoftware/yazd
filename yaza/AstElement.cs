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
                e.DefineSymbols(currentScope);
            }
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            foreach (var e in _elements)
            {
                e.Layout(currentScope, ctx);
            }
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            foreach (var e in _elements)
            {
                e.Generate(currentScope, ctx);
            }
        }

    }


    // Like a container but contains symbol definitions
    public class AstScope : AstContainer
    {
        public AstScope(string name) : base(name)
        {
        }


        // Define a symbols value
        public void Define(string symbol, ExprNode node)
        {
            // Check not already defined in an outer scope
            var outerScope = ContainingScope;
            if (outerScope != null && outerScope.IsSymbolDefined(symbol))
            {
                throw new InvalidOperationException(string.Format("The symbol '{0}' is already defined in an outer scope", symbol));
            }

            // Check if already defined
            ExprNode existing;
            if (_symbols.TryGetValue(symbol, out existing))
            {
                throw new InvalidOperationException(string.Format("Duplicate symbol: '{0}'", symbol));
            }

            // Store it
            _symbols[symbol] = node;
        }

        // Check if a symbol is defined in this scope or any outer scope
        public bool IsSymbolDefined(string symbol)
        {
            return FindSymbol(symbol) != null;
        }

        // Find the definition of a symbol
        public ExprNode FindSymbol(string symbol)
        {
            ExprNode def;
            if (_symbols.TryGetValue(symbol, out def))
                return def;

            var outerScope = ContainingScope;
            if (outerScope == null)
                return null;

            return outerScope.FindSymbol(symbol);
        }

        // Dictionary of symbols in this scope
        Dictionary<string, ExprNode> _symbols = new Dictionary<string, ExprNode>(StringComparer.InvariantCultureIgnoreCase);

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
                w.WriteLine($"    {kv.Key,20}: 0x{kv.Value.Evaluate(this):X4}");
            }
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            base.DefineSymbols(this);
        }

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            base.Layout(this, ctx);
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
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
            try
            {
                ctx.SetOrg(_address = _expr.Evaluate(currentScope));
            }
            catch (CodeException x)
            {
                Log.Error(x);
            }
        }

        public override void Generate(AstScope currentScope, GenerateContext ctx)
        {
            ctx.ListTo(SourcePosition);
            ctx.SetOrg(SourcePosition, _address);
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

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- EQU '{_name}' {SourcePosition.AstDesc()}");
            _value.Dump(w, indent + 1);
        }

        public override void DefineSymbols(AstScope currentScope)
        {
            currentScope.Define(_name, _value);
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

        public override void Layout(AstScope currentScope, LayoutContext ctx)
        {
            var sb = new StringBuilder();

            sb.Append(_mnemonic);

            try
            {
                for (int i = 0; i < _operands.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");
                    else
                        sb.Append(" ");

                    var o = _operands[i];
                    switch (o.GetAddressingMode(currentScope))
                    {
                        case AddressingMode.DerefImmediate:
                            sb.Append($"(?)");
                            break;

                        case AddressingMode.DerefRegister:
                            sb.Append($"({o.GetRegister()})");
                            break;

                        case AddressingMode.DerefRegisterPlusImmediate:
                            sb.Append($"({o.GetRegister()}+?)");
                            break;

                        case AddressingMode.Immediate:
                            sb.Append($"?");
                            break;
                        case AddressingMode.Register:
                            sb.Append($"{o.GetRegister()}");
                            break;

                        case AddressingMode.RegisterPlusImmediate:
                            sb.Append($"{o.GetRegister()}+?");
                            break;
                    }
                }
            }
            catch (CodeException x)
            {
                Log.Error(x);
                return;
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
                    switch (o.GetAddressingMode(currentScope))
                    {
                        case AddressingMode.Immediate:
                        case AddressingMode.DerefImmediate:
                        case AddressingMode.DerefRegisterPlusImmediate:
                        case AddressingMode.RegisterPlusImmediate:
                            // Create the list if not already
                            if (immediateValues == null)
                                immediateValues = new List<int>();

                            // Get the immediate value
                            try
                            {
                                immediateValues.Add(o.GetImmediateValue(currentScope));
                            }
                            catch (CodeException x)
                            {
                                Log.Error(x);
                                immediateValues.Add(0);
                            }
                            break;
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
}