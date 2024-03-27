using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public struct ParseTree
{
    public List<FunctionDefinitionNode> Functions;

    public ParseTree()
    {
        Functions = new();
    }

    public override string ToString() => $"Program{Functions.ToLines().Indent()}";
}

public struct FunctionDefinitionNode
{
    public Position Position;

    public string? Name;
    public List<VariableNode> Arguments;
    public ITypeNode ReturnType;
    public BlockNode Block;

    public FunctionDefinitionNode(Position position)
    {
        Position = position;
        ReturnType = new TypeNoneNode();
        Arguments = new();
        Block = new();
    }

    public override string ToString() 
        => $"Function\n{Name.Indent()}{Arguments.ToLines().Indent()}\n{("-> " + ReturnType.ToString()).Indent()}\n{Block.Indent()}";
}

public struct VariableNode
{
    public ITypeNode? Type;
    public string? Name;

    public override string ToString() => $"{Name} :: {Type}";
}

public struct TupleNode
{
    public List<VariableNode> Elements;

    public TupleNode()
    {
        Elements = new();
    }

    public override string ToString() => $"Tuple{Elements.ToLines().Indent()}";
}
