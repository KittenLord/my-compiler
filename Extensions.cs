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
                        TokenType.Eq, TokenType.Neq, TokenType.Mod,
                        TokenType.Ls, TokenType.Gr, TokenType.GrEq, TokenType.LsEq);
    }

    public static int GetBinaryOperatorPrecedence(this Token token)
    {
        return token.Type switch
        {
            TokenType.Eq or TokenType.Neq or
            TokenType.Ls or TokenType.LsEq or
            TokenType.Gr or TokenType.GrEq => 0,

            TokenType.Plus or TokenType.Minus => 1,

            TokenType.Mod => 2,

            TokenType.Mul or TokenType.Div => 3,

            _ => int.MinValue,
        };
    }

    public static string Indent(this string text)
    {
        return string.Join("\n", text.Split('\n').Select(s => "    " + s));
    }
}
