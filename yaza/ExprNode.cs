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
        public ExprNode(SourcePosition pos)
        {
            SourcePosition = pos;
        }

        public SourcePosition SourcePosition { get; private set; }

        public abstract void Dump(TextWriter w, int indent);
        public virtual long EvaluateNumber(AstScope scope)
        {
            // Evaluate 
            var val = Evaluate(scope);

            // Try to convert to long
            try
            {
                return Convert.ToInt64(val);
            }
            catch
            {
                throw new CodeException($"Can't convert {Utils.TypeName(val)} to number.");
            }
        }
        public virtual object Evaluate(AstScope scope)
        {
            return EvaluateNumber(scope);
        }
        public virtual AddressingMode GetAddressingMode(AstScope scope) { return AddressingMode.Invalid; }
        public virtual string GetRegister(AstScope scope) { return null; }
        public virtual string GetSubOp() { throw new InvalidOperationException(); }
        public virtual long GetImmediateValue(AstScope scope) { return EvaluateNumber(scope); }
    }

    public class ExprNodeParameterized : ExprNode
    {
        public ExprNodeParameterized(SourcePosition pos, string[] parameterNames, ExprNode body)
            : base(pos)
        {
            _parameterNames = parameterNames;
            _body = body;
        }

        public static string MakeSuffix(int parameterCount)
        {
            return $"/{parameterCount}";
        }

        public static string RemoveSuffix(string name)
        {
            var slash = name.IndexOf('/');
            if (slash < 0)
                return name;
            return name.Substring(0, slash);
        }

        string[] _parameterNames;
        ExprNode _body;

        public ExprNode Resolve(SourcePosition pos, ExprNode[] arguments)
        {
            return new ExprNodeParameterizedInstance(pos, _parameterNames, arguments, _body);
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- parameterized({string.Join(",", _parameterNames)})");
            _body.Dump(w, indent + 1);
        }

        public override long EvaluateNumber(AstScope scope)
        {
            throw new NotImplementedException();
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            throw new NotImplementedException();
        }

        public override long GetImmediateValue(AstScope scope)
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
        public ExprNodeParameterizedInstance(SourcePosition pos, string[] parameterNames, ExprNode[] arguments, ExprNode body)
            : base(pos)
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

        public override long EvaluateNumber(AstScope scope)
        {
            try
            {
                _scope.Container = scope;
                return _body.EvaluateNumber(_scope);
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

        public override long GetImmediateValue(AstScope scope)
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
        public ExprNodeLiteral(SourcePosition pos, long value)
            : base(pos)
        {
            _value = value;
        }

        long _value;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- literal {_value}");
        }

        public override long EvaluateNumber(AstScope scope)
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
        public ExprNodeDeferredValue(SourcePosition pos, string name)
            : base(pos)
        {
            _name = name;
        }

        long? _value;
        string _name;

        public void Resolve(long value)
        {
            _value = value;
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- deferred value: {_value}");
        }

        public override long EvaluateNumber(AstScope scope)
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
        public ExprNodeUnary(SourcePosition pos)
            : base(pos)
        {
        }

        public string OpName;
        public Func<long, long> Operator;
        public ExprNode RHS;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- operator {OpName}");
            RHS.Dump(w, indent + 1);
        }

        public override long EvaluateNumber(AstScope scope)
        {
            return Operator(RHS.EvaluateNumber(scope));
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            if (RHS.GetAddressingMode(scope) == AddressingMode.Immediate)
                return AddressingMode.Immediate;
            else
                return AddressingMode.Invalid;
        }

        public static long OpLogicalNot(long val)
        {
            return val == 0 ? 1 : 0;
        }

        public static long OpBitwiseComplement(long val)
        {
            return ~val;
        }

        public static long OpNegate(long val)
        {
            return -val;
        }

    }

    public class ExprNodeBinary : ExprNode
    {
        public ExprNodeBinary(SourcePosition pos)
            : base(pos)
        {
        }

        public string OpName;
        public Func<long, long, long> Operator;
        public ExprNode LHS;
        public ExprNode RHS;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- operator {OpName}");
            LHS.Dump(w, indent + 1);
            RHS.Dump(w, indent + 1);
        }

        public override long EvaluateNumber(AstScope scope)
        {
            return Operator(LHS.EvaluateNumber(scope), RHS.EvaluateNumber(scope));
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            if (RHS.GetAddressingMode(scope) == AddressingMode.Immediate && LHS.GetAddressingMode(scope) == AddressingMode.Immediate)
                return AddressingMode.Immediate;
            else
                return AddressingMode.Invalid;
        }

        public static long OpMul(long a, long b)
        {
            return a * b;
        }

        public static long OpDiv(long a, long b)
        {
            return a / b;
        }

        public static long OpMod(long a, long b)
        {
            return a % b;
        }

        public static long OpAdd(long a, long b)
        {
            return a + b;
        }

        public static long OpLogicalAnd(long a, long b)
        {
            return (a != 0 && b != 0) ? 1 : 0;
        }

        public static long OpLogicalOr(long a, long b)
        {
            return (a != 0 || b != 0) ? 1 : 0;
        }

        public static long OpBitwiseAnd(long a, long b)
        {
            return a & b;
        }

        public static long OpBitwiseOr(long a, long b)
        {
            return a | b;
        }

        public static long OpBitwiseXor(long a, long b)
        {
            return a ^ b;
        }

        public static long OpShl(long a, long b)
        {
            return a << (int)b;
        }

        public static long OpShr(long a, long b)
        {
            return a >> (int)b;
        }

        public static long OpEQ(long a, long b)
        {
            return a == b ? 1 : 0;
        }

        public static long OpNE(long a, long b)
        {
            return a != b ? 1 : 0;
        }

        public static long OpGT(long a, long b)
        {
            return a > b ? 1 : 0;
        }

        public static long OpLT(long a, long b)
        {
            return a < b ? 1 : 0;
        }

        public static long OpGE(long a, long b)
        {
            return a >= b ? 1 : 0;
        }

        public static long OpLE(long a, long b)
        {
            return a <= b ? 1 : 0;
        }
    }

    public class ExprNodeAdd : ExprNodeBinary
    {
        public ExprNodeAdd(SourcePosition pos)
            : base(pos)
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

        public override long GetImmediateValue(AstScope scope)
        {
            var lhsMode = LHS.GetAddressingMode(scope);
            var rhsMode = RHS.GetAddressingMode(scope);

            long val = 0;
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
        public ExprNodeTernery(SourcePosition pos)
            : base(pos)
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

        public override long EvaluateNumber(AstScope scope)
        {
            if (Condition.EvaluateNumber(scope) != 0)
            {
                return TrueValue.EvaluateNumber(scope);
            }
            else
            {
                return FalseValue.EvaluateNumber(scope);
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
        public ExprNodeIdentifier(SourcePosition pos, string name)
            : base(pos)
        {
            _name = name;
        }

        string _name;

        public ExprNode[] Arguments;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- identifier '{_name}'");
            if (Arguments != null)
            {
                w.WriteLine($"{Utils.Indent(indent + 1)}- args");
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
                var symbol = scope.FindSymbol(_name);
                if (symbol == null)
                {
                    throw new CodeException($"Unrecognized symbol: '{_name}'", SourcePosition);
                }
                var expr = symbol as ExprNode;
                if (expr == null)
                {
                    throw new CodeException($"Invalid expression: '{_name}' is not a value", SourcePosition);
                }
                return expr;
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
                return symbol.Resolve(SourcePosition, Arguments);
            }
        }

        public override object Evaluate(AstScope scope)
        {
            if (Arguments == null)
            {
                // Simple symbol
                var symbol = scope.FindSymbol(_name);
                if (symbol == null)
                {
                    throw new CodeException($"Unrecognized symbol: '{_name}'", SourcePosition);
                }
                return symbol;
            }

            return base.Evaluate(scope);
        }

        public override long EvaluateNumber(AstScope scope)
        {
            return FindSymbol(scope).EvaluateNumber(scope);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return FindSymbol(scope).GetAddressingMode(scope);
        }

        public override long GetImmediateValue(AstScope scope)
        {
            return FindSymbol(scope).GetImmediateValue(scope);
        }

        public override string GetRegister(AstScope scope)
        {
            return FindSymbol(scope).GetRegister(scope);
        }
    }

    public class ExprNodeMember : ExprNode
    {
        public ExprNodeMember(SourcePosition pos, string name, ExprNode lhs)
            : base(pos)
        {
            _lhs = lhs;
            _name = name;
        }

        ExprNode _lhs;
        string _name;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- member '.{_name}' of:");
            _lhs.Dump(w, indent + 1);
        }

        public override object Evaluate(AstScope scope)
        {
            // Get the LHS (which must be a type)
            var lhsVal = _lhs.Evaluate(scope);
            var type = lhsVal as AstType;
            if (type == null)
            {
                var fd = lhsVal as AstFieldDefinition;
                if (fd != null)
                {
                    type = fd.Type;
                }
                else
                {
                    throw new CodeException($"LHS of member operator '.{_name}' is not a type or field ", SourcePosition);
                }
            }

            // Get the field definition
            var fieldDefinition = type.FindField(_name);
            if (fieldDefinition == null)
                throw new CodeException($"The type '{type.Name}' does not contain a field named '{_name}'", SourcePosition);

            // Return the field definition
            return fieldDefinition;
        }

        public override long EvaluateNumber(AstScope scope)
        {
            // Get the LHS (which must be a type or another field definition)
            var lhsVal = _lhs.Evaluate(scope);

            // Direct member of a type?
            var lhsType = lhsVal as AstType;
            if (lhsType != null)
            {
                // Get the field
                var fieldDefinition = lhsType.FindField(_name);
                if (fieldDefinition == null)
                    throw new CodeException($"The type '{lhsType.Name}' does not contain a field named '{_name}'", SourcePosition);

                // Return the field's offset
                return fieldDefinition.Offset;
            }

            // Member of member?
            var lhsMember = lhsVal as AstFieldDefinition;
            if (lhsMember != null)
            {
                var fieldDefinition = lhsMember.Type.FindField(_name);
                if (fieldDefinition == null)
                    throw new CodeException($"The type '{lhsMember.Type.Name}' does not contain a field named '{_name}'", SourcePosition);

                return _lhs.EvaluateNumber(scope) + fieldDefinition.Offset;
            }

            throw new CodeException($"LHS of member operator '.{_name}' is not a type or field ", SourcePosition);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }
    }

    public class ExprNodeSizeOf : ExprNode
    {
        public ExprNodeSizeOf(SourcePosition pos, ExprNode target)
            : base(pos)
        {
            _target = target;
        }

        ExprNode _target;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- sizeof");
            _target.Dump(w, indent + 1);
        }


        public override long EvaluateNumber(AstScope scope)
        {
            // Get the LHS (which must be a type or another field definition)
            var targetVal = _target.Evaluate(scope);

            // Is the target a type declaration
            var targetType = targetVal as AstType;
            if (targetType != null)
                return targetType.SizeOf;

            // Is the target a member
            var targetField = targetVal as AstFieldDefinition;
            if (targetField != null)
                return targetField.Type.SizeOf;

            throw new CodeException($"Invalid use of sizeof operator with '{Utils.TypeName(targetVal)}' parameter", SourcePosition);
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }
    }

    public class ExprNodeRegisterOrFlag : ExprNode
    {
        public ExprNodeRegisterOrFlag(SourcePosition pos, string name)
            : base(pos)
        {
            _name = name;
        }

        string _name;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- reg/cond '{_name}'");
        }

        public override long EvaluateNumber(AstScope scope)
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
        public ExprNodeDeref(SourcePosition pos)
            : base(pos)
        {
        }

        public ExprNode Pointer;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- deref pointer");
            Pointer.Dump(w, indent + 1);
        }

        public override long EvaluateNumber(AstScope scope)
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

        public override long GetImmediateValue(AstScope scope)
        {
            return Pointer.GetImmediateValue(scope);
        }
    }

    public class ExprNodeIP : ExprNode
    {
        public ExprNodeIP(bool allowOverride)
            : base(null)
        {
            _allowOverride = allowOverride;
        }

        bool _allowOverride;
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

        public override long EvaluateNumber(AstScope scope)
        {
            // Temporarily overridden? (See ExprNodeEquWrapper)
            if (_allowOverride && scope.ipOverride.HasValue)
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

        public override long GetImmediateValue(AstScope scope)
        {
            // Temporarily overridden? (See ExprNodeEquWrapper)
            if (_allowOverride && scope.ipOverride.HasValue)
                return scope.ipOverride.Value;

            return _generateContext.ip;
        }
    }

    public class ExprNodeOFS : ExprNode
    {
        public ExprNodeOFS(bool allowOverride)
            : base(null)
        {
            _allowOverride = allowOverride;
        }

        bool _allowOverride;
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
            w.WriteLine($"{Utils.Indent(indent)}- ofs '$ofs' pointer");
        }

        public override long EvaluateNumber(AstScope scope)
        {
            // Temporarily overridden? (See ExprNodeEquWrapper)
            if (_allowOverride && scope.opOverride.HasValue)
                return scope.opOverride.Value;

            // Generating
            if (_generateContext != null)
                return _generateContext.op;

            // Layouting?
            if (_layoutContext != null)
                return _layoutContext.op;

            // No OFS for you!
            throw new CodeException("Symbol $ofs can't be resolved at this time");
        }

        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }

        public override long GetImmediateValue(AstScope scope)
        {
            // Temporarily overridden? (See ExprNodeEquWrapper)
            if (_allowOverride && scope.opOverride.HasValue)
                return scope.opOverride.Value;

            return _generateContext.op;
        }
    }

    // Represents a "sub op" operand.  eg: the "RES " part of "LD A,RES 0,(IX+1)"
    public class ExprNodeSubOp : ExprNode
    {
        public ExprNodeSubOp(SourcePosition pos, string subop)
            : base(pos)
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

        public override long EvaluateNumber(AstScope scope)
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

        public override long GetImmediateValue(AstScope scope)
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
    public class ExprNodeEquWrapper : ExprNode
    {
        public ExprNodeEquWrapper(SourcePosition pos, ExprNode rhs, string name)
            : base(pos)
        {
            _rhs = rhs;
            _ipOverride = 0;
            _name = name;
        }

        public void SetOverrides(int ipOverride, int opOverride)
        {
            _ipOverride = ipOverride;
            _opOverride = opOverride;
        }

        ExprNode _rhs;
        int _ipOverride;
        int _opOverride;
        string _name;
        bool _recursionCheck;

        void CheckForRecursion()
        {
            if (_recursionCheck)
            {
                throw new CodeException($"Recursive symbol reference: {_name}", SourcePosition);
            }
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- EQU wrapper $ => 0x{_ipOverride:X4} $ofs => 0x{_opOverride:X4}");
            _rhs.Dump(w, indent + 1);
        }

        public override long EvaluateNumber(AstScope scope)
        {
            CheckForRecursion();

            var ipSave = scope.ipOverride;
            var opSave = scope.opOverride;
            _recursionCheck = true;
            try
            {
                scope.ipOverride = _ipOverride;
                scope.opOverride = _opOverride;
                return _rhs.EvaluateNumber(scope);
            }
            finally
            {
                scope.ipOverride = ipSave;
                scope.opOverride = opSave;
                _recursionCheck = false;
            }
        }
        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            CheckForRecursion();

            _recursionCheck = true;
            try
            {
                return _rhs.GetAddressingMode(scope);
            }
            finally
            {
                _recursionCheck = false;
            }
        }
        public override string GetRegister(AstScope scope)
        {
            return _rhs.GetRegister(scope);
        }
        public override string GetSubOp()
        {
            return _rhs.GetSubOp();
        }
        public override long GetImmediateValue(AstScope scope)
        {
            CheckForRecursion();

            var ipSave = scope.ipOverride;
            var opSave = scope.opOverride;
            _recursionCheck = true;
            try
            {
                scope.ipOverride = _ipOverride;
                scope.opOverride = _opOverride;
                return _rhs.GetImmediateValue(scope);
            }
            finally
            {
                scope.ipOverride = ipSave;
                scope.opOverride = opSave;
                _recursionCheck = false;
            }
        }
    }


    // This expression node temporarily overrides the value of $ while
    // the RHS expression is evaluated.  This is used by EQU definitions
    // to resolve $ to the loation the EQU was defined - not the location
    // it was invoked from.  See also ExprNodeIP
    public class ExprNodeIsDefined : ExprNode
    {
        public ExprNodeIsDefined(SourcePosition pos, string symbolName)
            : base(pos)
        {
            _symbolName = symbolName;
        }

        string _symbolName;

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- defined({_symbolName})");
        }

        public override long EvaluateNumber(AstScope scope)
        {
            if (scope.IsSymbolDefined(_symbolName, true))
                return 1;
            else
                return 0;
        }
        public override AddressingMode GetAddressingMode(AstScope scope)
        {
            return AddressingMode.Immediate;
        }
        public override string GetRegister(AstScope scope)
        {
            return null;
        }
        public override string GetSubOp()
        {
            return null;
        }
        public override long GetImmediateValue(AstScope scope)
        {
            return EvaluateNumber(scope);
        }
    }

    // Represents an array of values [a,b,c]
    public class ExprNodeArray : ExprNode
    {
        public ExprNodeArray(SourcePosition pos)
            : base(pos)
        {
        }

        public void AddElement(ExprNode elem)
        {
            _elements.Add(elem);
        }

        List<ExprNode> _elements = new List<ExprNode>();

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- array");
            foreach (var e in _elements)
            {
                e.Dump(w, indent + 1);
            }
        }

        public override object Evaluate(AstScope scope)
        {
            return _elements.Select(x => x.Evaluate(scope)).ToArray();
        }
    }

    // Represents a map of values { x: a, y: b }
    public class ExprNodeMap : ExprNode
    {
        public ExprNodeMap(SourcePosition pos)
            : base(pos)
        {
        }

        public void AddEntry(string name, ExprNode value)
        {
            _entries.Add(name, value);
        }

        public bool ContainsEntry(string name)
        {
            return _entries.ContainsKey(name);
        }

        Dictionary<string, ExprNode> _entries = new Dictionary<string, ExprNode>(StringComparer.InvariantCultureIgnoreCase);

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- array");
            foreach (var e in _entries)
            {
                w.WriteLine($"{Utils.Indent(indent + 1)}- \"{e.Key}\"");
                e.Value.Dump(w, indent + 2);
            }
        }
    }

    // Uninitialized data node ie: '?'
    public class ExprNodeUninitialized : ExprNode
    {
        public ExprNodeUninitialized(SourcePosition pos)
            : base(pos)
        {
        }

        public override void Dump(TextWriter w, int indent)
        {
            w.WriteLine($"{Utils.Indent(indent)}- uninitialized data '?'");
        }

        public override object Evaluate(AstScope scope)
        {
            return this;        // as good as anything, just need a marker
        }
    }

    /*
    public class ExprNodeDataDeclaration : ExprNode
    {
        public ExprNodeDataDeclaration(SourcePosition pos)
            : base(pos)
        {
        }
    }
    */
}