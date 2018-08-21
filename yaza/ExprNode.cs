using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    [Flags]
    public enum AddressingMode
    {
        Deref = 0x80,
        SubOp = 0x40,

        Invalid = 0,

        Immediate = 0x01,
        Register = 0x02,
        RegisterPlusImmediate = Register | Immediate,

        Mask = 0x07,


        /*

        DerefImmediate = Deref | Immediate,
        DerefRegister = Deref | Register,
        DerefRegisterPlusImmediate = Deref | RegisterPlusImmediate,
        */
    }

    public abstract class ExprNode
    {
        // Source position of this expression (only really usd on root expression elements)
        public SourcePosition SourcePosition;

        public abstract void Dump(TextWriter w, int indent);
        public abstract int Evaluate(AstScope scope);
        public abstract AddressingMode GetAddressingMode(AstScope scope);
        public virtual string GetRegister() { return null; }
        public virtual string GetSubOp() { throw new InvalidOperationException(); }
        public virtual int GetImmediateValue(AstScope scope) { return Evaluate(scope); }
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
            SourcePosition = pos;
        }

        int? _value;
        string _name;

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

            throw new CodeException($"The value of symbol '{_name}' can't been resolved", SourcePosition);
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

        public override int GetImmediateValue(AstScope scope)
        {
            foreach (var n in _nodes)
            {
                if ((n.GetAddressingMode(scope) & AddressingMode.Immediate) != 0)
                {
                        return n.GetImmediateValue(scope);
                }
            }

            throw new NotImplementedException("Internal error");
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
        public ExprNodeIdentifier(SourcePosition position, string name)
        {
            _name = name;
            SourcePosition = position;
        }

        string _name;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- identifier '{_name}'");
        }

        public override int Evaluate(AstScope scope)
        {
            var symbolDefinition = scope.FindSymbol(_name);
            if (symbolDefinition != null)
                return symbolDefinition.Evaluate(scope);

            throw new CodeException($"unknown symbol: '{_name}'", SourcePosition);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            var symbolDefinition = scope.FindSymbol(_name);
            if (symbolDefinition != null)
                return symbolDefinition.GetAddressingMode(scope);

            throw new CodeException($"unknown symbol: '{_name}'", SourcePosition);
        }
    }

    public class ExprNodeRegisterOrFlag : ExprNode
    {
        public ExprNodeRegisterOrFlag(SourcePosition position, string name)
        {
            _name = name;
            SourcePosition = position;
        }

        string _name;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- reg/cond '{_name}'");
        }

        public override int Evaluate(AstScope scope)
        {
            throw new CodeException("'{_name}' can't be evaluated at compile time", SourcePosition);
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
            SourcePosition = position;
        }

        public ExprNode Pointer;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- deref pointer");
            Pointer.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            throw new CodeException("pointer dereference operator can't be evaluated at compile time", SourcePosition);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            var mode = Pointer.GetAddressingMode(scope);

            if ((mode & (AddressingMode.Deref | AddressingMode.SubOp)) == 0)
                return mode | AddressingMode.Deref;

            return AddressingMode.Invalid;
        }

        public override string GetRegister()
        {
            return Pointer.GetRegister();
        }

        public override int GetImmediateValue(AstScope scope)
        {
            return Pointer.GetImmediateValue(scope);
        }
    }

    public class ExprNodeIP : ExprNode
    {
        public ExprNodeIP()
        {
        }

        GenerateContext _generateContext;
        LayoutContext _layoutContext;

        public void SetContext(GenerateContext ctx)
        {
            _generateContext = ctx;
            _layoutContext = null;
        }

        public void SetContext(LayoutContext ctx)
        {
            _layoutContext = ctx;
            _generateContext = null;
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- ip '$' pointer");
        }

        public override int Evaluate(AstScope scope)
        {
            if (_generateContext != null)
                return _generateContext.ip;

            if (_layoutContext != null)
                return _layoutContext.ip;

            throw new CodeException("Symbol $ can't be resolved at this time");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }

        public override int GetImmediateValue(AstScope scope)
        {
            return _generateContext.ip;
        }
    }

    // Represents a "sub op" operand.  eg: the "RES " part of "LD A,RES 0,(IX+1)"
    public class ExprNodeSubOp : ExprNode
    {
        public ExprNodeSubOp(string subop)
        {
            _subop = subop;
        }

        string _subop;

        public ExprNode RHS;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- sub-op '{_subop}'");
            RHS.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            throw new CodeException("sub-op operator can't be evaluated at compile time", SourcePosition);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.SubOp | RHS.GetAddressingMode(scope);
        }

        public override string GetRegister()
        {
            return RHS.GetRegister();
        }

        public override int GetImmediateValue(AstScope scope)
        {
            return RHS.GetImmediateValue(scope);
        }

        public override string GetSubOp()
        {
            return _subop;
        }

    }
}