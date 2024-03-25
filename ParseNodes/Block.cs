using System;
using System.Linq;
using System.Collections.Generic;

namespace MyCompiler.Parsing;

public interface IBlockLineNode 
{

}

public struct BlockNode : IExpression
{
    public List<IBlockLineNode> Lines;
    public bool ReturnLast;

    public BlockNode()
    {
        Lines = new();
    }

    public override string ToString()
    {
        var lines = string.Join("", Lines.Select(s => "\n" + s)).Indent();
        return $"Block {(ReturnLast ? "Returns" : "")}{lines}";
    }
}

public struct DeclarationNode : IBlockLineNode
{
    public Token? Id;
    public TypeNode? Type;

    public IExpression? Expr;

    public override string ToString()
    {
        return $"Declare {Id} with type {(Type?.ToString() ?? "<Auto>")}\n{Expr}";
    }
}

public struct MutationNode : IBlockLineNode
{
    public Token? Id;
    public Token? Operator;
    public IExpression? Expr;

    public override string ToString()
    {
        return $"Mutate {Id} with {Operator} to\n{Expr}";
    }
}

public struct IfNode : IBlockLineNode
{
    public IExpression? Condition;
    public BlockNode Block;

    public override string ToString()
    {
        return $"If\n{Condition}\n{Block}";
    }
}

public struct ElseNode : IBlockLineNode
{
    public BlockNode Block;

    public override string ToString()
    {
        return $"Else\n{Block}";
    }
}

public struct ElseIfNode : IBlockLineNode
{
    public IExpression? Condition;
    public BlockNode Block;

    public override string ToString()
    {
        return $"Else if\n{Condition}\n{Block}";
    }
}
