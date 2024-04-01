using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

// Technically it is an AST, but whatever
public class ParseTree
{
    public List<FunctionDefinitionNode> Functions;
    public List<TypeDefinitionNode> Types;
    public List<LetDefinitionNode> Variables;

    public ParseTree()
    {
        Functions = new();
        Types = new();
        Variables = new();
    }

    public override string ToString() => $"Program{Functions.ToLines().Indent()}{Types.ToLines().Indent()}{Variables.ToLines().Indent()}";
}

public class FunctionDefinitionNode
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
        => $"Function\n{Name?.Indent() ?? ""}{Arguments.ToLines().Indent()}\n{("-> " + ReturnType.ToString()).Indent()}\n{Block.Indent()}";
}

public class VariableNode
{
    public ITypeNode? Type;
    public string? Name;

    public override string ToString() => $"{Name} : {Type}";
}

public class TupleNode
{
    public List<VariableNode> Elements;

    public TupleNode()
    {
        Elements = new();
    }

    public override string ToString() => $"Tuple{Elements.ToLines().Indent()}";
}

public class TypeDefinitionNode
{
    public Position Position;

    public string? Name;
    public List<VariableNode> Members;

    public TypeDefinitionNode()
    {
        Members = new();
    }

    public override string ToString() => $"Type{Members.ToLines().Indent()}";
}
