using System;
using System.Linq;
using System.Collections.Generic;

namespace MyCompiler.Parsing;

public interface IBlockLineNode 
{
    public bool Return { get; set; }
}

public struct BlockNode
{
    public List<IBlockLineNode> Lines;

    public BlockNode()
    {
        Lines = new();
    }
}

public struct DeclarationNode : IBlockLineNode
{
    public Token? Id;

    public bool Return { get; set; }
}
