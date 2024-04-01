using System;
using System.Collections.Generic;
using MyCompiler.Parsing;

namespace MyCompiler.Analysis;

public class TypeInfo
{
    public bool None = false;

    public string Name;
    public int Size;

    public TypeInfo(string name, int size = 0)
    {
        Name = name;
        Size = size;
        None = false;
    }

    public TypeInfo()
    {
        None = true;
    }

    public static bool operator ==(TypeInfo a, TypeInfo b)
        => a.Name == b.Name;

    public static bool operator !=(TypeInfo a, TypeInfo b)
        => a.Name != b.Name;
}

public static class BIType
{
    public static readonly TypeInfo Int = new TypeInfo("int", 8);
    public static readonly TypeInfo Float = new TypeInfo("float", 8);
    public static readonly TypeInfo Bool = new TypeInfo("bool", 8);
    public static readonly TypeInfo String = new TypeInfo("string", 8);
}
