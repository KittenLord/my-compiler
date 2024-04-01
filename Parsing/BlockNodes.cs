using System;
using System.Collections.Generic;
using System.Linq;

namespace MyCompiler.Parsing;

public class BlockNode : IAccessible
{
    public List<IExpressionNode> Lines;

    public bool ReturnLast;
    public ITypeNode ReturnType;

    public BlockNode()
    {
        Lines = new();
        ReturnLast = false;
        ReturnType = new TypeAutoNode();
    }

    public override string ToString() => $"Block\n{"-> " + ReturnType.Indent()}{Lines.ToLines().Indent()}";
}

public interface IBlockLineNode : IExpressionNode {}

public class LetDefinitionNode : IBlockLineNode
{
    public Position Position;

    public ITypeNode Type;
    public string? Name;
    public IExpressionNode Expression;

    public override string ToString() => $"Let\n{Name?.Indent() ?? ""} : {Type}\n{Expression.Indent()}";
}

public class MutationNode : IBlockLineNode
{
    public IAccessible Access;
    public Token Operator;
    public IExpressionNode Expression;

    public override string ToString() => $"Mut\n{Access.Indent() ?? ""} {Operator}\n{Expression.Indent()}";
}

public class IfNode : IBlockLineNode
{
    public IExpressionNode Condition;
    public BlockNode Block;

    public override string ToString() => $"If\n{Condition.Indent()}\n{Block.Indent()}";
}

public class ElseNode : IBlockLineNode
{
    public BlockNode Block;

    public override string ToString() => $"Else\n{Block.Indent()}";
}

public class ElseIfNode : IBlockLineNode
{
    public IExpressionNode Condition;
    public BlockNode Block;

    public override string ToString() => $"Else If\n{Condition.Indent()}\n{Block.Indent()}";
}

public class WhileNode : IBlockLineNode
{
    public bool Do;
    public IExpressionNode Condition;
    public BlockNode Block;

    public override string ToString() => $"While{(Do ? " Do" : "")}\n{Condition.Indent()}\n{Block.Indent()}";
}
