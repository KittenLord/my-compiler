using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public interface ILiteralNode {}

public class IdLiteralNode : ILiteralNode
{
    public string Id;

    public IdLiteralNode(string id)
    {
        Id = id;
    }

    public override string ToString() => $"{Id} : id";
}

public class ValueLiteralNode : ILiteralNode
{
    public ITypeNode Type;
    public string Value;

    public ValueLiteralNode(string value, ITypeNode type)
    {
        Value = value;
        Type = type;
    }

    public override string ToString() => $"{Value} : {Type}";
}
