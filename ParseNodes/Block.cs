using System;
using System.Linq;
using System.Collections.Generic;

namespace MyCompiler.Parsing;

public interface IBlockLineNode 
{
    public bool Return { get; set; }
}

public struct BlockNode : IExpression
{
    public List<IBlockLineNode> Lines;

    public BlockNode()
    {
        Lines = new();
    }

    public override string ToString()
    {
        var lines = string.Join("", Lines.Select(s => "\n" + s)).Indent();
        return $"Block{lines}";
    }
}

public struct DeclarationNode : IBlockLineNode
{
    public Token? Id;
    public TypeNode? Type;

    public IExpression? Expr;

    public bool Return { get; set; }

    public override string ToString()
    {
        return $"Declare {Id} with type {(Type?.ToString() ?? "<Auto>")}\n{Expr}";
    }
}
