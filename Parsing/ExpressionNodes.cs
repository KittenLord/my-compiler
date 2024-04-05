using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public interface IExpressionNode 
{
    public bool Contains(Func<IExpressionNode, bool> predicate);
    public ITypeNode GetType();
}

public interface IAccessible : IExpressionNode {}

public class OperatorExpressionNode : IExpressionNode
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

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this) || predicate(Left) || predicate(Right);

    public ITypeNode GetType()
        => new TypeNode(MyCompiler.Analysis.OperatorResult.Get(Operator, Left.GetType(), Right.GetType()));
}


public class UnaryOperatorExpressionNode : IAccessible
{
    public Token Operator;
    public IExpressionNode Base;

    public UnaryOperatorExpressionNode(Token op, IExpressionNode b)
    {
        Operator = op;
        Base = b;
    }

    public override string ToString() => $"{Operator.Type}\n{Base.Indent()}";

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this) || predicate(Base);

    public ITypeNode GetType()
        => Base.GetType();
}

public class LiteralExpressionNode : IAccessible
{
    public ILiteralNode Literal;

    public LiteralExpressionNode(ILiteralNode literal)
    {
        Literal = literal;
    }

    public override string ToString() => $"{Literal}";

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this);

    public ITypeNode GetType()
        => Literal.Type;
}

public class ArrayAccessorNode : IAccessible
{
    public IAccessible Base;
    public IExpressionNode Index;

    public ArrayAccessorNode(IAccessible b, IExpressionNode index)
    {
        Base = b;
        Index = index;
    }

    public override string ToString() => $"{Base}\n*\n{Index.Indent()}";

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this) || predicate(Base) || predicate(Index);

    public ITypeNode GetType()
    {
        var t = Base.GetType();
        if(t is not TypeNode type) return t;
        type = type.Copy();
        type.Mods.Dequeue();
        return type;
    }
}

public class FuncAccessorNode : IAccessible
{
    public IAccessible Base;
    public List<IExpressionNode> Arguments;

    public FuncAccessorNode(IAccessible a)
    {
        Base = a;
        Arguments = new();
    }

    public override string ToString() => $"{Base}\n${Arguments.ToLines().Indent()}";

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this) || Arguments.Any(arg => predicate(arg));

    public ITypeNode GetType()
        => new TypeAutoNode();
}

public class PointerAccessorNode : IAccessible
{
    public IAccessible Base;

    public PointerAccessorNode(IAccessible b)
    {
        Base = b;
    }

    public override string ToString() => $"{Base}\n@";

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this);

    public ITypeNode GetType()
    {
        var t = Base.GetType();
        if(t is not TypeNode type) return t;
        type = type.Copy();
        type.Mods.Dequeue();
        return type;
    }
}

public class MemberAccessorNode : IAccessible
{
    public IAccessible Base;
    public string Member;

    public MemberAccessorNode(IAccessible b, string member)
    {
        Base = b;
        Member = member;
    }

    public override string ToString() => $"{Base}\n. {Member}";

    public bool Contains(Func<IExpressionNode, bool> predicate)
        => predicate(this) || predicate(Base);

    public ITypeNode GetType()
        => new TypeAutoNode();
}
