using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    // Base class for all AST elements
    public abstract class AstElement
    {
        // The container holding this element
        public AstContainer Container;

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

        public abstract void Dump(int indent);
    }

    // Container for other AST elements
    public class AstContainer : AstElement
    {
        public AstContainer()
        {
        }

        public virtual void AddElement(AstElement element)
        {
            element.Container = this;
            _elements.Add(element);
        }

        protected List<AstElement> _elements = new List<AstElement>();


        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- CONTAINER");
            foreach (var e in _elements)
            {
                e.Dump(indent + 1);
            }
        }
    }


    // Like a container but contains symbol definitions
    public class AstScope : AstContainer
    {
        public AstScope()
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
            if (_symbols.ContainsKey(symbol))
                return true;

            var outerScope = ContainingScope;
            if (outerScope == null)
                return false;

            return outerScope.IsSymbolDefined(symbol);
        }

        // Dictionary of symbols in this scope
        Dictionary<string, ExprNode> _symbols = new Dictionary<string, ExprNode>(StringComparer.InvariantCultureIgnoreCase);

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- SCOPE");
            foreach (var e in _elements)
            {
                e.Dump(indent + 1);
            }
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- ORG");
            _expr.Dump(indent + 1);
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

        public override void Dump(int indent)
        {
            foreach (var v in _values)
            {
                v.Dump(indent);
            }
        }
    }

    // "DB" directive
    public class AstDbElement : AstDxElement
    {
        public AstDbElement()
        {
        }

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- DB");
            base.Dump(indent + 1);
        }
    }

    // "DW" directive
    public class AstDwElement : AstDxElement
    {
        public AstDwElement()
        {
        }

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- DW");
            base.Dump(indent + 1);
        }
    }

    // Label
    public class AstLabel : AstElement
    {
        public AstLabel(string name)
        {
            _name = name;
        }

        string _name;
        ExprNode _value = new ExprNodeDeferredValue();

        public string Name => _name;
        public ExprNode Value => _value;

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- LABEL '{_name}'");
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- EQU '{_name}'");
            _value.Dump(indent + 1);
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- INCLUDE '{_filename}'");
            _content.Dump(indent + 1);
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- INCBIN '{_filename}'");

            for (int i = 0; i < _data.Length; i++)
            {
                if ((i % 16) == 0)
                    Console.Write($"{Utils.Indent(indent + 1)}- ");
                Console.Write("{0:X2}", _data[i]);
            }

            if (_data.Length > 0)
                Console.WriteLine();
        }
    }

    public class AstInstruction : AstElement
    {
        public AstInstruction(string mnemonic)
        {
            _mnemonic = mnemonic;
        }

        public void AddOperand(ExprNode operand)
        {
            _operands.Add(operand);
        }

        string _mnemonic;
        List<ExprNode> _operands = new List<ExprNode>();

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- {_mnemonic.ToUpperInvariant()}");
            foreach (var o in _operands)
            {
                o.Dump(indent + 1);
            }
        }
    }
}