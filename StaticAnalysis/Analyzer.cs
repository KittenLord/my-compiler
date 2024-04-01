using System;
using System.Collections.Generic;
using MyCompiler.Parsing;
using MyCompiler.Analysis;

namespace MyCompiler;

public static class AnalysisError
{
    public static string FunctionSameName(string name)
        => $"A function named \"{name}\" already exists";

    public static string VariableSameName(string name)
        => $"A variable named \"{name}\" already exists";

    public static string TypeSameName(string name)
        => $"A type named \"{name}\" already exists";

    public const string GlobalVarAuto
        = "Global variables must have their types explicitly specified";
}

public class Analyzer
{
    public bool Success = true;
    public List<AttachedMessage> Errors;

    private ParseTree Tree;

    public Analyzer(ParseTree tree)
    {
        Tree = tree;
        Errors = new();
    }

    private void Error(string message, Position position)
    {
        Success = false;
        Errors.Add(new AttachedMessage(message, position));
    }

    private void CheckSameFunctionNames()
    {
        for(int i = 0; i < Tree.Functions.Count; i++)
        {
            for(int j = i + 1; j < Tree.Functions.Count; j++)
            {
                if(Tree.Functions[i].Name == Tree.Functions[j].Name)
                    Error(AnalysisError.FunctionSameName(Tree.Functions[j].Name ?? ""), Tree.Functions[j].Position);
            }
        }
    }

    private void CheckSameTypeNames()
    {
        for(int i = 0; i < Tree.Types.Count; i++)
        {
            for(int j = i + 1; j < Tree.Types.Count; j++)
            {
                if(Tree.Types[i].Name == Tree.Types[j].Name)
                    Error(AnalysisError.TypeSameName(Tree.Types[j].Name ?? ""), Tree.Types[j].Position);
            }
        }
    }

    private void CheckGlobalVariableAuto()
    {
        foreach(var variable in Tree.Variables)
        {
            if(variable.Type is TypeAutoNode) 
                Error(AnalysisError.GlobalVarAuto, variable.Position);
        }
    }

    public List<TypeInfo> Types = new();

    private void GenerateTypes()
    {
    }

    // I'm just now realizing that navigating the parsetree will be a nightmare
    public void Analyze()
    {
        CheckSameFunctionNames();
        CheckSameTypeNames();
        CheckGlobalVariableAuto();
    }
}
