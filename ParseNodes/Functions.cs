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
        string args = string.Join("", Params.Select(p => "\n\t" + p.ToString()));
        return $"Function {Id?.Value ?? "N/A"}{args}{(ReturnType is not null ? $"\n\t-> {ReturnType?.Value}" : "")}";
    }
}

public struct FnDefParamNode
{
    public Token? Type;
    public Token? Id;

    public override string ToString()
    {
        return $"({Type?.Value ?? "N/A"} {Id?.Value ?? "N/A"})";
    }
}
