using System;
using System.Linq;
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

    public static string TypeMemberNotFixedSize(string type, string member)
        => $"Type's {type} member {member} must be a fixed-size value, i.e primitive/array/pointer";

    public static string TypeDoesntExist(string type)
        => $"Type {type} doesn't exist";
}

public class Analyzer
{
    public bool Success = true;
    public List<AttachedMessage> Errors;

    public List<TypeInfo> Types = new();

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

    private void GenerateTypes()
    {
        foreach(var t in BIType.List) Types.Add(t);

        var validList = new List<TypeDefinitionNode>();
        foreach(var typedef in Tree.Types)
        {
            if(typedef.Name is null || Types.Any(c => c.Name == typedef.Name)) { continue; }
            validList.Add(typedef);
            Types.Add(new TypeInfo(typedef.Name));
        }

        foreach(var typedef in validList)
        {
            var type = Types.Find(t => t.Name == typedef.Name)!;
            foreach(var member in typedef.Members)
            {
                if(member.Name is null) continue;
                if(member.Type is not TypeNode node) continue;

                if(!Types.Any(extype => extype.Name == node.Type.Name))
                {
                    Error(AnalysisError.TypeDoesntExist(node.Type.Name), member.Position);
                    continue;
                }

                // Pointer/array
                if(node.Mods.Count > 0) 
                { 
                    type.Size += 8; 
                    type.Members.Add(new TypeMemberInfo(node, member.Name)); 
                    continue; 
                }

                // Primitive
                if(BIType.List.Any(prim => prim.Name == node.Type.Name))
                {
                    type.Size += 8;
                    type.Members.Add(new TypeMemberInfo(node, member.Name)); 
                    continue;
                }

                Error(AnalysisError.TypeMemberNotFixedSize(type.Name, member.Name), member.Position);
            }
        }
    }

    // I'm just now realizing that navigating the parsetree will be a nightmare
    public void Analyze()
    {
        CheckSameFunctionNames();
        CheckSameTypeNames();
        CheckGlobalVariableAuto();
        GenerateTypes();
    }
}
