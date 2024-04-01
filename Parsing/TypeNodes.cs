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

public class TypeNode : ITypeNode
{
    public TypeInfo Type;

    public TypeNode(TypeInfo type)
    {
        Type = type;
    }

    public TypeNode(string name)
    {
        Type = new TypeInfo(name);
    }

    public override string ToString() => Type.Name;

    public override bool Equals(object? obj)
    {
        if(obj is not TypeNode type) return false;
        return Type.Name == type.Type.Name;
    }
}

public class TypeArrayNode : ITypeNode
{
    public ITypeNode Base;

    public TypeArrayNode(ITypeNode baseType)
    {
        Base = baseType;
    }

    public override string ToString() => Base.ToString() + "[]";

    public override bool Equals(object? obj)
    {
        if(obj is not TypeArrayNode type) return false;
        return Base.Equals(type.Base);
    }
}

public class TypePointerNode : ITypeNode
{
    public ITypeNode Base;

    public TypePointerNode(ITypeNode baseType)
    {
        Base = baseType;
    }

    public override string ToString() => Base.ToString() + "@";

    public override bool Equals(object? obj)
    {
        if(obj is not TypePointerNode type) return false;
        return Base.Equals(type.Base);
    }
}
