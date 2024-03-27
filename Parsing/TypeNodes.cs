namespace MyCompiler.Parsing;

public interface ITypeNode {}
public struct TypeNoneNode : ITypeNode 
{
    public override string ToString() => "NONE";
}

public struct TypeNode : ITypeNode
{
    public string Name;

    public TypeNode(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;
}

public struct TypeArrayNode : ITypeNode
{
    public ITypeNode Base;

    public TypeArrayNode(ITypeNode baseType)
    {
        Base = baseType;
    }

    public override string ToString() => Base.ToString() + "[]";
}

public struct TypePointerNode : ITypeNode
{
    public ITypeNode Base;

    public TypePointerNode(ITypeNode baseType)
    {
        Base = baseType;
    }

    public override string ToString() => Base.ToString() + "@";
}
