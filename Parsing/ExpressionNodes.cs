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
}

public struct LiteralExpressionNode : IAccessible
{
    public Token Literal;

    public LiteralExpressionNode(Token literal)
    {
        Literal = literal;
    }
}

public struct ArrayAccessorNode : IAccessible
{
    public IAccessible Base;
    public IExpressionNode Index;
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
}

public struct PointerAccessorNode : IAccessible
{
    public IAccessible Base;
}

public struct MemberAccessorNode : IAccessible
{
    public IAccessible Base;
    public string Member;
}
