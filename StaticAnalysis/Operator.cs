namespace MyCompiler.Analysis;

public static class OperatorResult
{
    public static TypeInfo Get(Token op, TypeInfo a, TypeInfo b)
    {
        if(op.Is(TokenType.Eq, TokenType.Neq))
        {
            return (a == b && 
                   (a == BIType.Int || a == BIType.Float || a == BIType.Bool) && 
                   (b == BIType.Int || b == BIType.Float || b == BIType.Bool)) 
                ? BIType.Bool 
                : new TypeInfo();
        }

        if(op.Is(TokenType.Gr, TokenType.Ls, TokenType.LsEq, TokenType.GrEq))
        {
            return((a == BIType.Int || a == BIType.Float) && 
                   (b == BIType.Int || b == BIType.Float)) 
                ? BIType.Bool 
                : new TypeInfo();
        }

        if(a == BIType.Int && b == BIType.Int) return BIType.Int;
        if(a == BIType.Float || b == BIType.Float) return BIType.Float;

        return new TypeInfo();
    }
}
