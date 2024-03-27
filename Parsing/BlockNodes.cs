using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public struct BlockNode : IAccessible
{
    public List<IExpressionNode> Lines;
    public bool ReturnLast;

    public BlockNode()
    {
        Lines = new();
        ReturnLast = false;
    }

    public override string ToString() => $"Block{(ReturnLast ? " ->" : "")}{Lines.ToLines().Indent()}";
}

public interface IBlockLineNode : IExpressionNode {}

public struct LetDefinitionNode : IBlockLineNode
{
    public ITypeNode Type;
    public string? Name;
    public IExpressionNode Expression;

    public override string ToString() => $"Let\n{Name?.Indent() ?? ""} : {Type}\n{Expression.Indent()}";
}
