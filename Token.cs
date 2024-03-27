using System.Linq;

namespace MyCompiler;

public enum TokenType
{
    EOF, // End of the token stream 
    Invalid, // an invalid token
    Let, // let
    Mut, // mut
    Free, // free
    Fn, // fn
    If, // if
    Else, // else
    For, // for
    From, // from
    To, // to
    While, // while
    Do, // do while
    True, // true
    False, // false
    Type, // type
    Return, // return
    Id, // identifier, i.e. variable/function name, type

    Number, // 12312, 0x0000

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

    RetArrow, // ->

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
    Pointer, // @

    String, // " iabwdiuwab uidbawudb a"
}

public struct Token
{
    public TokenType Type;
    public string Value;

    public int Line;
    public int Char;

    public override string ToString() => this.Type.ToString();

    public Token(TokenType type, int line, int charn, string value = "")
    {
        Type = type;
        Value = value;
        Line = line;
        Char = charn;
    }

    public Position Position => new Position(Line, Char);

    public bool Is(params TokenType[] types)
    {
        var type = Type;
        return types.Any(t => t == type);
    }

    public bool IsNot(params TokenType[] types)
    {
        var type = Type;
        return types.All(t => t != type);
    }

    public bool IsEnd() => Type == TokenType.EOF;
}

public struct Position 
{
    public int Line;
    public int Char;

    public override string ToString()
        => $"[ {Line} : {Char} ]";
        
    public Position(int line, int charn)
    {
        Line = line;
        Char = charn;
    }
}
