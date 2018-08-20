using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public enum AddressingMode
    {
        Invalid,
        Immediate,
        Register,
        RegisterPlusImmediate,
        DerefImmediate,
        DerefRegister,
        DerefRegisterPlusImmediate,
    }

    public abstract class ExprNode
    {
        public abstract void Dump(TextWriter w, int indent);
        public abstract int Evaluate(AstScope scope);
        public abstract AddressingMode GetAddressingMode(AstScope scope);
        public virtual string GetRegister() { return null; }
    }

    public class ExprNodeLiteral : ExprNode
    {
        public ExprNodeLiteral(int value)
        {
            _value = value;
        }

        int _value;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- literal {_value}");
        }

        public override int Evaluate(AstScope scope)
        {
            return _value;
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }
    }

    public class ExprNodeDeferredValue : ExprNode
    {
        public ExprNodeDeferredValue(string name, SourcePosition pos)
        {
            _name = name;
            _position = pos;
        }

        int? _value;
        string _name;
        SourcePosition _position;

        public void Resolve(int value)
        {
            _value = value;
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- deferred value: {_value}");
        }

        public override int Evaluate(AstScope scope)
        {
            if (_value.HasValue)
                return _value.Value;

            throw new CodeException(_position, $"The value of symbol '{_name}' can't been resolved");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }
    }

    public class ExprNodeUnary : ExprNode
    {
        public ExprNodeUnary()
        {
        }

        public string OpName;
        public Func<int, int> Operator;
        public ExprNode RHS;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- operator {OpName}");
            RHS.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            return Operator(RHS.Evaluate(scope));
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            if (RHS.GetAddressingMode(scope) == AddressingMode.Immediate)
                return AddressingMode.Immediate;
            else
                return AddressingMode.Invalid;
        }

        public static int OpLogicalNot(int val)
        {
            return val == 0 ? 1 : 0;
        }

        public static int OpBitwiseComplement(int val)
        {
            return ~val;
        }

        public static int OpNegate(int val)
        {
            return -val;
        }

    }

    public class ExprNodeBinary : ExprNode
    {
        public ExprNodeBinary()
        {
        }

        public string OpName;
        public Func<int, int, int> Operator;
        public ExprNode LHS;
        public ExprNode RHS;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- operator {OpName}");
            LHS.Dump(w, indent + 1);
            RHS.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            return Operator(LHS.Evaluate(scope), RHS.Evaluate(scope));
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            if (RHS.GetAddressingMode(scope) == AddressingMode.Immediate && LHS.GetAddressingMode(scope) == AddressingMode.Immediate)
                return AddressingMode.Immediate;
            else
                return AddressingMode.Invalid;
        }

        public static int OpMul(int a, int b)
        {
            return a * b;
        }

        public static int OpDiv(int a, int b)
        {
            return a * b;
        }

        public static int OpMod(int a, int b)
        {
            return a * b;
        }

        public static int OpLogicalAnd(int a, int b)
        {
            return (a != 0 && b != 0) ? 1 : 0;
        }

        public static int OpLogicalOr(int a, int b)
        {
            return (a != 0 || b != 0) ? 1 : 0;
        }

        public static int OpBitwiseAnd(int a, int b)
        {
            return a & b;
        }

        public static int OpBitwiseOr(int a, int b)
        {
            return a | b;
        }

        public static int OpBitwiseXor(int a, int b)
        {
            return a ^ b;
        }

        public static int OpShl(int a, int b)
        {
            return a << b;
        }

        public static int OpShr(int a, int b)
        {
            return a >> b;
        }

        public static int OpEQ(int a, int b)
        {
            return a == b ? 1 : 0;
        }

        public static int OpNE(int a, int b)
        {
            return a != b ? 1 : 0;
        }

        public static int OpGT(int a, int b)
        {
            return a > b ? 1 : 0;
        }

        public static int OpLT(int a, int b)
        {
            return a < b ? 1 : 0;
        }

        public static int OpGE(int a, int b)
        {
            return a >= b ? 1 : 0;
        }

        public static int OpLE(int a, int b)
        {
            return a <= b ? 1 : 0;
        }
    }

    public class ExprNodeTernery : ExprNode
    {
        public ExprNodeTernery()
        {
        }

        public ExprNode Condition;
        public ExprNode TrueValue;
        public ExprNode FalseValue;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- ternery");
            w.WriteLine($"{Utils.Indent(indent + 1)}- condition");
            Condition.Dump(w, indent + 2);
            w.WriteLine($"{Utils.Indent(indent + 1)}- trueValue");
            TrueValue.Dump(w, indent + 2);
            w.WriteLine($"{Utils.Indent(indent + 1)}- falseValue");
            FalseValue.Dump(w, indent + 2);
        }

        public override int Evaluate(AstScope scope)
        {
            if (Condition.Evaluate(scope) != 0)
            {
                return TrueValue.Evaluate(scope);
            }
            else
            {
                return FalseValue.Evaluate(scope);
            }
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            if (TrueValue.GetAddressingMode(scope) == AddressingMode.Immediate && FalseValue.GetAddressingMode(scope) == AddressingMode.Immediate)
                return AddressingMode.Immediate;
            else
                return AddressingMode.Invalid;
        }

    }

    public class ExprNodeAddLTR : ExprNode
    {
        public ExprNodeAddLTR()
        {
        }

        public void AddNode(ExprNode node)
        {
            _nodes.Add(node);
        }

        List<ExprNode> _nodes = new List<ExprNode>();


        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- add ltr");
            foreach (var n in _nodes)
            {
                n.Dump(w, indent + 1);
            }
        }

        public override int Evaluate(AstScope scope)
        {
            int val = 0;
            foreach (var n in _nodes)
            {
                val += n.Evaluate(scope);
            }
            return val;
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            int registers = 0;
            int immediates = 0;
            foreach (var n in _nodes)
            {
                switch (n.GetAddressingMode(scope))
                {
                    case AddressingMode.Immediate:
                        immediates++;
                        break;

                    case AddressingMode.Register:
                        registers++;
                        break;

                    default:
                        return AddressingMode.Invalid;
                }
            }

            if (registers == 0)
            {
                if (immediates > 0)
                    return AddressingMode.Immediate;
                else
                    return AddressingMode.Invalid;
            }
            else
            {
                if (immediates > 0)
                    return AddressingMode.RegisterPlusImmediate;
                else
                    return AddressingMode.Register;
            }
        }

        public override string GetRegister()
        {
            foreach (var n in _nodes)
            {
                var r = n.GetRegister();
                if (r != null)
                    return r;
            }
            return null;
        }
    }


    public class ExprNodeIdentifier : ExprNode
    {
        public ExprNodeIdentifier(SourcePosition pos, string name)
        {
            _name = name;
            _sourcePosition = pos;
        }

        string _name;
        SourcePosition _sourcePosition;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- identifier '{_name}'");
        }

        public override int Evaluate(AstScope scope)
        {
            var symbolDefinition = scope.FindSymbol(_name);
            if (symbolDefinition != null)
                return symbolDefinition.Evaluate(scope);

            throw new CodeException(_sourcePosition, $"unknown symbol: '{_name}'");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            var symbolDefinition = scope.FindSymbol(_name);
            if (symbolDefinition != null)
                return symbolDefinition.GetAddressingMode(scope);

            throw new CodeException(_sourcePosition, $"unknown symbol: '{_name}'");
        }
    }

    public class ExprNodeRegisterOrFlag : ExprNode
    {
        public ExprNodeRegisterOrFlag(SourcePosition position, string name)
        {
            _name = name;
            _position = position;
        }

        string _name;
        SourcePosition _position;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- reg/cond '{_name}'");
        }

        public override int Evaluate(AstScope scope)
        {
            throw new CodeException(_position, "'{_name}' can't be evaluated at compile time");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Register;
        }

        public override string GetRegister()
        {
            return _name;
        }
    }

    public class ExprNodeDeref : ExprNode
    {
        public ExprNodeDeref(SourcePosition position)
        {
            _position = position;
        }

        public ExprNode Pointer;
        SourcePosition _position;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- deref pointer");
            Pointer.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            throw new CodeException(_position, "pointer dereference operator can't be evaluated at compile time");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            switch (Pointer.GetAddressingMode(scope))
            {
                case AddressingMode.DerefImmediate:
                    return AddressingMode.DerefImmediate;

                case AddressingMode.Register:
                    return AddressingMode.DerefRegister;

                case AddressingMode.RegisterPlusImmediate:
                    return AddressingMode.DerefRegisterPlusImmediate;
            }

            return AddressingMode.Invalid;
        }

        public override string GetRegister()
        {
            return Pointer.GetRegister();
        }
    }
}