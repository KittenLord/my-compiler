namespace MyCompiler;

public enum TokenType
{
    EOF, // End of the token stream 
    Invalid, // an invalid token
    Let, // let
    Fn, // fn
    If, // if
    For, // for
    While, // while
    True, // true
    False, // false
    Return, // return
    Id, // identifier, i.e. variable/function name, type

    Number, // 12312, 1_000_000, 0x0000

    Plus, // +
    Minus, // - 
    Mul, // *
    Div, // /

    Mod, // %
    ModNeg, // %%

    PlusAsgn, // +=
    MinusAsgn, // -=
    MulAsgn, // *=
    DivAsgn, // /=
    ModAsgn, // %=
    ModNegAsgn, // %%=

    Not, // !
    Assign, // =
    Eq, // ==
    Neq, // !=
    Gr, // >
    GrEq, // >=
    Ls, // <
    LsEq, // <=

    LParen, // (
    RParen, // )

    LBrack, // [
    RBrack, // ]

    LCurly, // {
    RCurly, // }

    Semi, // ;
    Comma, // ,
    Dot, // .

    String, // " iabwdiuwab uidbawudb a"
}

public struct Token
{
    public TokenType Type;
    public string Value;

    public int Line;
    public int Char;

    public Token(TokenType type, int line, int charn, string value = "")
    {
        Type = type;
        Value = value;
        Line = line;
        Char = charn;
    }

    public override string ToString()
    {
        return $"{Type.ToString()}{(Value == " " ? "" : $" ({Value})")} at [{Line} : {Char}]";
    }
}
