using System;
using System.Linq;
using System.Collections.Generic;

namespace MyCompiler.Parsing;

public interface IExpression : IBlockLineNode
{

}

public interface ILiteral
{

}

public struct OpExprNode : IExpression
{
    public Token Operator;

    public IExpression Lhs;
    public IExpression Rhs;

    public override string ToString()
    {
        var body = $"{Lhs}\n{Rhs}".Indent();
        return $"{Operator} ->\n{body}";
    }
}

public struct UnOpExtrNode : IExpression
{
    public Token Operator;

    public IExpression Expr;

    public override string ToString()
    {
        var body = $"{Expr}".Indent();
        return $"{Operator} ->\n{body}";
    }
}

public struct ChainLitExprNode : IExpression
{
    public List<ILiteral> Chain = new();
    public ChainLitExprNode() {}

    public override string ToString()
    {
        return "Chain" + string.Join("", Chain.Select(s => "\n." + s.ToString())).Indent();
    }
}

public struct LiteralExprNode : ILiteral
{
    public Token Literal;

    public override string ToString() 
    {
        return Literal.Value;
    }
}

public struct FuncExprNode : ILiteral
{
    public Token Func;

    public override string ToString()
    {
        return $"Func\n{string.Join("", Args.Select(s => "\n->" + s.ToString())).Indent()}";
    }

    public List<IExpression> Args = new();
    public FuncExprNode() {}
}

public struct ArrayExprNode : ILiteral
{
    public ILiteral Array;
    public IExpression Index;

    public override string ToString()
    {
        return $"{Array} index {Index}";
    }
}
