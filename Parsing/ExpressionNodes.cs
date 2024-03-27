using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public interface IExpressionNode {}
public interface IAccessible : IExpressionNode {}

public struct OperatorExpressionNode : IExpressionNode
{
    public Token Operator;
    public IExpressionNode Left;
    public IExpressionNode Right;

    public OperatorExpressionNode(Token op, IExpressionNode lhs, IExpressionNode rhs)
    {
        Operator = op;
        Left = lhs;
        Right = rhs;
    }

    public override string ToString() => $"{Operator.Type}\n{Left.Indent()}\n{Right.Indent()}";
}

public struct LiteralExpressionNode : IAccessible
{
    public ILiteralNode Literal;

    public LiteralExpressionNode(ILiteralNode literal)
    {
        Literal = literal;
    }

    public override string ToString() => $"{Literal}";
}

public struct ArrayAccessorNode : IAccessible
{
    public IAccessible Base;
    public IExpressionNode Index;

    public ArrayAccessorNode(IAccessible b, IExpressionNode index)
    {
        Base = b;
        Index = index;
    }

    public override string ToString() => $"{Base} *\n{Index.Indent()}";
}

public struct FuncAccessorNode : IAccessible
{
    public IAccessible Base;
    public List<IExpressionNode> Arguments;

    public FuncAccessorNode(IAccessible a)
    {
        Base = a;
        Arguments = new();
    }

    public override string ToString() => $"{Base} ${Arguments.ToLines().Indent()}";
}

public struct PointerAccessorNode : IAccessible
{
    public IAccessible Base;

    public PointerAccessorNode(IAccessible b)
    {
        Base = b;
    }

    public override string ToString() => $"{Base} @";
}

public struct MemberAccessorNode : IAccessible
{
    public IAccessible Base;
    public string Member;

    public MemberAccessorNode(IAccessible b, string member)
    {
        Base = b;
        Member = member;
    }

    public override string ToString() => $"{Base} -> {Member}";
}
