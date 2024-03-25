using System;
using System.Linq;
using System.Collections.Generic;
using MyCompiler.Parsing;

namespace MyCompiler;

public static class Extensions
{
    public static bool IsBinaryOperator(this Token token)
    {
        return token.Is(TokenType.Plus, TokenType.Minus, TokenType.Mul, TokenType.Div,
                        TokenType.Eq, TokenType.Neq, TokenType.Mod, TokenType.ModNeg,
                        TokenType.Ls, TokenType.Gr, TokenType.GrEq, TokenType.LsEq);
    }

    public static bool IsMutOperator(this Token token)
    {
        return token.Is(TokenType.Assign, 
                        TokenType.PlusAsgn, TokenType.MinusAsgn, TokenType.MulAsgn, TokenType.DivAsgn,
                        TokenType.ModAsgn, TokenType.ModNegAsgn);
    }

    public static bool IsValue(this Token token)
    {
        return token.Is(TokenType.Id, TokenType.Number, TokenType.String, TokenType.True, TokenType.False);
    }

    public static int GetBinaryOperatorPrecedence(this Token token)
    {
        return token.Type switch
        {
            TokenType.Eq or TokenType.Neq or
            TokenType.Ls or TokenType.LsEq or
            TokenType.Gr or TokenType.GrEq => 0,

            TokenType.Plus or TokenType.Minus => 1,

            TokenType.Mod or TokenType.ModNeg => 2,

            TokenType.Mul or TokenType.Div => 3,

            _ => int.MinValue,
        };
    }

    public static string Indent(this string text)
    {
        return string.Join("\n", text.Split('\n').Select(s => "    " + s));
    }
}
