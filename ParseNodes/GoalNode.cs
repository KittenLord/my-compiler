using System;
using System.Collections.Generic;

namespace MyCompiler.Parsing;

public struct GoalNode
{
    public List<FnDefNode> Functions { get; private set; } = new();     

    public GoalNode() {}

    public override string ToString()
    {
        return string.Join("\n\n", Functions);
    }
}
