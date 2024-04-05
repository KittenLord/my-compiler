using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public interface ILiteralNode { public ITypeNode Type { get; set; } }

public class IdLiteralNode : ILiteralNode
{
    public ITypeNode Type { get; set; }
    public string Id;

    public IdLiteralNode(ITypeNode type, string id)
    {
        Type = type;
        Id = id;
    }

    public override string ToString() => $"{Id} : id";
}

public class ValueLiteralNode : ILiteralNode
{
    public ITypeNode Type { get; set; }
    public string Value;

    public ValueLiteralNode(string value, ITypeNode type)
    {
        Value = value;
        Type = type;
    }

    public override string ToString() => $"{Value} : {Type}";
}
