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

public struct MutationNode : IBlockLineNode
{
    public string Name;
    public Token Operator;
    public IExpressionNode Expression;

    public override string ToString() => $"Mut\n{Name?.Indent() ?? ""} {Operator}\n{Expression.Indent()}";
}

public struct IfNode : IBlockLineNode
{
    public IExpressionNode Condition;
    public BlockNode Block;

    public override string ToString() => $"If\n{Condition.Indent()}\n{Block.Indent()}";
}

public struct ElseNode : IBlockLineNode
{
    public BlockNode Block;

    public override string ToString() => $"Else\n{Block.Indent()}";
}

public struct ElseIfNode : IBlockLineNode
{
    public IExpressionNode Condition;
    public BlockNode Block;

    public override string ToString() => $"Else If\n{Condition.Indent()}\n{Block.Indent()}";
}

public struct WhileNode : IBlockLineNode
{
    public bool Do;
    public IExpressionNode Condition;
    public BlockNode Block;

    public override string ToString() => $"While{(Do ? " Do" : "")}\n{Condition.Indent()}\n{Block.Indent()}";
}
