using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public interface ILiteralNode {}

public struct IdLiteralNode : ILiteralNode
{
    public string Id;

    public IdLiteralNode(string id)
    {
        Id = id;
    }

    public override string ToString() => $"{Id} : id";
}

public struct NumberLiteralNode : ILiteralNode
{
    public string Value;

    public NumberLiteralNode(string value)
    {
        Value = value;
    }

    public override string ToString() => $"{Value} : number";
}

public struct BoolLiteralNode : ILiteralNode
{
    public bool Value;

    public BoolLiteralNode(bool value)
    {
        Value = value;
    }

    public override string ToString() => $"{Value} : bool";
}

public struct StringLiteralValue : ILiteralNode
{
    public string Value;

    public StringLiteralValue(string value)
    {
        Value = value;
    }

    public override string ToString() => $"\"{Value}\" : string";
}
