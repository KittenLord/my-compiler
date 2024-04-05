using System.Collections.Generic;
using MyCompiler.Analysis;

namespace MyCompiler.Parsing;

public interface ITypeNode {}

public class TypeNoneNode : ITypeNode 
{
    public override string ToString() => "NONE";
}

public class TypeAutoNode : ITypeNode
{
    public override string ToString() => "AUTO";
}

public enum TypeMod { Array, Pointer }

public class TypeNode : ITypeNode
{
    public TypeInfo Type;
    public Queue<TypeMod> Mods;

    public TypeNode(TypeInfo type)
    {
        Type = type;
        Mods = new();
    }

    public TypeNode(string name)
    {
        Type = new TypeInfo(name);
        Mods = new();
    }

    public override string ToString() => Type.Name;

    public override bool Equals(object? obj)
    {
        if(obj is not TypeNode type) return false;
        return Type.Name == type.Type.Name;
    }
}
