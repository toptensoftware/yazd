using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace yaza
{
    public abstract class ExprNode
    {
        public abstract void Dump(int indent);
    }

    public class ExprNodeLiteral : ExprNode
    {
        public ExprNodeLiteral(int value)
        {
            _value = value;
        }

        int _value;

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- literal {_value}");
        }
    }

    public class ExprNodeDeferredValue : ExprNode
    {
        public ExprNodeDeferredValue()
        {

        }

        int? _value;

        public void Resolve(int value)
        {
            _value = value;
        }

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- deferred value: {_value}");
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- operator {OpName}");
            RHS.Dump(indent + 1);
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- operator {OpName}");
            LHS.Dump(indent + 1);
            RHS.Dump(indent + 1);
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

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- ternery");
            Console.WriteLine($"{Utils.Indent(indent + 1)}- condition");
            Condition.Dump(indent + 2);
            Console.WriteLine($"{Utils.Indent(indent + 1)}- trueValue");
            TrueValue.Dump(indent + 2);
            Console.WriteLine($"{Utils.Indent(indent + 1)}- falseValue");
            FalseValue.Dump(indent + 2);
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


        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- add ltr");
            foreach (var n in _nodes)
            {
                n.Dump(indent + 1);
            }
        }
    }


    public class ExprNodeIdentifier : ExprNode
    {
        public ExprNodeIdentifier(string name)
        {
            _name = name;
        }

        string _name;

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- identifier '{_name}'");
        }
    }

    public class ExprNodeRegisterOrFlag : ExprNode
    {
        public ExprNodeRegisterOrFlag(string name)
        {
            _name = name;
        }

        string _name;

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- reg/cond '{_name}'");
        }
    }

    public class ExprNodeDeref : ExprNode
    {
        public ExprNodeDeref()
        {
        }

        public ExprNode Pointer;

        public override void Dump(int indent)
        {
            Console.WriteLine($"{Utils.Indent(indent)}- deref pointer");
            Pointer.Dump(indent + 1);
        }
    }
}