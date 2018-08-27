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

    public interface ISymbolValue
    {
        void Dump(TextWriter w, int indent);
    }

    public abstract class ExprNode : ISymbolValue
    {
        // Source position of this expression (only really usd on root expression elements)
        public SourcePosition SourcePosition;

        public abstract void Dump(TextWriter w, int indent);
        public abstract int Evaluate(AstScope scope);
        public abstract AddressingMode GetAddressingMode(AstScope scope);
        public virtual string GetRegister(AstScope scope) { return null; }
        public virtual string GetSubOp() { throw new InvalidOperationException(); }
        public virtual int GetImmediateValue(AstScope scope) { return Evaluate(scope); }
    }

    public class ExprNodeParameterized : ExprNode
    {
        public ExprNodeParameterized(string[] parameterNames, ExprNode body)
        {
            _parameterNames = parameterNames;
            _body = body;
        }

        public static string MakeSuffix(int parameterCount)
        {
            return $"/{parameterCount}";
        }

        string[] _parameterNames;
        ExprNode _body;

        public ExprNode Resolve(ExprNode[] arguments)
        {
            return new ExprNodeParameterizedInstance(_parameterNames, arguments, _body);
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- parameterized({string.Join(",", _parameterNames)})");
            _body.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            throw new NotImplementedException();
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            throw new NotImplementedException();
        }

        public override int GetImmediateValue(AstScope scope)
        {
            throw new NotImplementedException();
        }

        public override string GetRegister(AstScope scope)
        {
            throw new NotImplementedException();
        }
    }

    public class ExprNodeParameterizedInstance : ExprNode
    {
        public ExprNodeParameterizedInstance(string[] parameterNames, ExprNode[] arguments, ExprNode body)
        {
            _scope = new AstScope("parameterized equate");
            for (int i = 0; i < parameterNames.Length; i++)
            {
                _scope.Define(parameterNames[i], arguments[i]);
            }
            _body = body;
        }

        AstScope _scope;
        ExprNode _body;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- parameterized [resolved]");
            _body.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            try
            {
                _scope.Container= scope;
                return _body.Evaluate(_scope);
            }
            finally
            {
                _scope.Container = null;
            }
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            try
            {
                _scope.Container = scope;
                return _body.GetAddressingMode(_scope);
            }
            finally
            {
                _scope.Container = null;
            }
        }

        public override int GetImmediateValue(AstScope scope)
        {
            try
            {
                _scope.Container = scope;
                return _body.GetImmediateValue(_scope);
            }
            finally
            {
                _scope.Container = null;
            }
        }

        public override string GetRegister(AstScope scope)
        {
            try
            {
                _scope.Container = scope;
                return _body.GetRegister(_scope);
            }
            finally
            {
                _scope.Container = null;
            }
        }
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
            return a / b;
        }

        public static int OpMod(int a, int b)
        {
            return a % b;
        }

        public static int OpAdd(int a, int b)
        {
            return a + b;
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

    public class ExprNodeAdd : ExprNodeBinary
    {
        public ExprNodeAdd()
        {
            OpName = "+";
            Operator = ExprNodeBinary.OpAdd;
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            var lhsMode = LHS.GetAddressingMode(scope);
            var rhsMode = RHS.GetAddressingMode(scope);

            if (lhsMode == AddressingMode.Immediate && rhsMode == AddressingMode.Immediate)
                return AddressingMode.Immediate;

            if (lhsMode == AddressingMode.Immediate && rhsMode == AddressingMode.Register)
                return AddressingMode.RegisterPlusImmediate;

            if (lhsMode == AddressingMode.Register && rhsMode == AddressingMode.Immediate)
                return AddressingMode.RegisterPlusImmediate;

            if (lhsMode == AddressingMode.Immediate && rhsMode == AddressingMode.RegisterPlusImmediate)
                return AddressingMode.RegisterPlusImmediate;

            if (lhsMode == AddressingMode.RegisterPlusImmediate && rhsMode == AddressingMode.Immediate)
                return AddressingMode.RegisterPlusImmediate;

            return AddressingMode.Invalid;
        }

        public override int GetImmediateValue(AstScope scope)
        {
            var lhsMode = LHS.GetAddressingMode(scope);
            var rhsMode = RHS.GetAddressingMode(scope);

            int val = 0;
            if ((lhsMode & AddressingMode.Immediate) != 0)
                val += LHS.GetImmediateValue(scope);
            if ((rhsMode & AddressingMode.Immediate) != 0)
                val += RHS.GetImmediateValue(scope);

            return val;
        }

        public override string GetRegister(AstScope scope)
        {
            return LHS.GetRegister(scope) ?? RHS.GetRegister(scope);
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

    public class ExprNodeIdentifier : ExprNode
    {
        public ExprNodeIdentifier(SourcePosition position, string name)
        {
            _name = name;
            SourcePosition = position;
        }

        string _name;

        public ExprNode[] Arguments;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- identifier '{_name}'");
            if (Arguments != null)
            {
                w.WriteLine($"{Utils.Indent(indent+1)}- args");
                foreach (var a in Arguments)
                {
                    a.Dump(w, indent + 2);
                }
            }
        }

        ExprNode FindSymbol(AstScope scope)
        {
            if (Arguments == null)
            {
                // Simple symbol
                var symbol = scope.FindSymbol(_name) as ExprNode;
                if (symbol == null)
                {
                    throw new CodeException($"Unrecognized symbol: '{_name}'", SourcePosition);
                }
                return symbol;
            }
            else
            {
                // Parameterized symbol
                var symbol = scope.FindSymbol(_name + ExprNodeParameterized.MakeSuffix(Arguments.Length)) as ExprNodeParameterized;
                if (symbol == null)
                {
                    throw new CodeException($"Unrecognized symbol: '{_name}' (with {Arguments.Length} arguments)", SourcePosition);
                }

                // Resolve it
                return symbol.Resolve(Arguments);
            }
        }

        public override int Evaluate(AstScope scope)
        {
            return FindSymbol(scope).Evaluate(scope);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return FindSymbol(scope).GetAddressingMode(scope);
        }

        public override int GetImmediateValue(AstScope scope)
        {
            return FindSymbol(scope).GetImmediateValue(scope);
        }

        public override string GetRegister(AstScope scope)
        {
            return FindSymbol(scope).GetRegister(scope);
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

        public override string GetRegister(AstScope scope)
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

        public override string GetRegister(AstScope scope)
        {
            return Pointer.GetRegister(scope);
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
            // Temporarily overridden? (See ExprNodeIPOverride)
            if (scope.ipOverride.HasValue)
                return scope.ipOverride.Value;

            // Generating
            if (_generateContext != null)
                return _generateContext.ip;

            // Layouting?
            if (_layoutContext != null)
                return _layoutContext.ip;

            // No IP for you!
            throw new CodeException("Symbol $ can't be resolved at this time");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }

        public override int GetImmediateValue(AstScope scope)
        {
            // Temporarily overridden? (See ExprNodeIPOverride)
            if (scope.ipOverride.HasValue)
                return scope.ipOverride.Value;

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

        public override string GetRegister(AstScope scope)
        {
            return RHS.GetRegister(scope);
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

    // This expression node temporarily overrides the value of $ while
    // the RHS expression is evaluated.  This is used by EQU definitions
    // to resolve $ to the loation the EQU was defined - not the location
    // it was invoked from.  See also ExprNodeIP
    public class ExprNodeIPOverride : ExprNode
    {
        public ExprNodeIPOverride(ExprNode rhs)
        {
            _rhs = rhs;
            _ipOverride = 0;
        }

        public void SetIPOverride(int ipOverride)
        {
            _ipOverride = ipOverride;
        }

        ExprNode _rhs;
        int _ipOverride;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- ip override $ => 0x{_ipOverride:X4}");
            _rhs.Dump(w, indent + 1);
        }

        public override int Evaluate(AstScope scope)
        {
            var save = scope.ipOverride;
            try
            {
                scope.ipOverride = _ipOverride;
                return _rhs.Evaluate(scope);
            }
            finally
            {
                scope.ipOverride = save;
            }
        }
        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return _rhs.GetAddressingMode(scope);
        }
        public override string GetRegister(AstScope scope)
        {
            return _rhs.GetRegister(scope);
        }
        public override string GetSubOp()
        {
            return _rhs.GetSubOp();
        }
        public override int GetImmediateValue(AstScope scope)
        {
            var save = scope.ipOverride;
            try
            {
                scope.ipOverride = _ipOverride;
                return _rhs.GetImmediateValue(scope);
            }
            finally
            {
                scope.ipOverride = save;
            }
        }
    }

}