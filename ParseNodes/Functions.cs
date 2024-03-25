using System;
using System.Linq;
using System.Collections.Generic;

namespace MyCompiler.Parsing;

public struct FnDefNode
{
    public Token? Id;
    public Token? ReturnType;
    public BlockNode? Block;
    public List<FnDefParamNode> Params;

    public FnDefNode()
    {
        Params = new();
    }

    public override string ToString()
    {
        string args = string.Join("", Params.Select(p => "\n" + p.ToString())).Indent();
        return $"Function {Id?.Value ?? "N/A"}{args}{(ReturnType is not null ? $"\n-> {ReturnType?.Value}".Indent() : "")}\n{Block.ToString().Indent()}";
    }
}

public struct FnDefParamNode
{
    public TypeNode? Type;
    public Token? Id;

    public override string ToString()
    {
        return $"({Type?.ToString() ?? "N/A"} {Id?.Value ?? "N/A"})";
    }
}

public struct TypeNode
{
    public Token? Base;
    public int Indexes;

    public override string ToString()
    {
        return Base?.Value + new string('*', Indexes);
    }
}
