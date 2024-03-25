using System;
using System.Collections.Generic;
using MyCompiler.Parsing;

namespace MyCompiler;

public struct ParseError
{
    public string Error;
    public int Line;
    public int Char;

    public override string ToString()
        => $"{Error}\n\tat [ {Line} : {Char} ]";
    
    public static ParseError SemicolonMissing(Token token)
        => new ParseError($"Missing a semicolon", token.Line, token.Char);

    public static ParseError NoFunctionReturnType(Token token)
        => new ParseError($"Function definition has -> operator, but no return type specified", token.Line, token.Char);

    public static ParseError InvalidFunctionDefinition(Token token)
        => new ParseError($"Invalid function definition. Functions are defined like this:\n\tfn name(type1 arg1, type2 arg2) -> retType {{ ... }}", token.Line, token.Char);

    public static ParseError InvalidVarDeclaration(Token token)
        => new ParseError($"Invalid variable declaration. Declare a variable like this:\n\tlet [type] name = value;", token.Line, token.Char);

    public static ParseError InvalidArgumentDefinition(Token token)
        => new ParseError($"Invalid argument definition", token.Line, token.Char);

    public static ParseError InvalidArrayDefinition(Token token)
        => new ParseError($"Invalid array definition:\n\twrong: string[]\n\tcorrect: string*", token.Line, token.Char);

    public static ParseError UnexpectedToken(Token token, params TokenType[] expected)
        => new ParseError($"Unexpected token {{{token.Type}}} (expected: [{string.Join(", ", expected)}])", token.Line, token.Char);

    public static ParseError MissingFunctionName(Token token)
        => new ParseError($"Missing function name", token.Line, token.Char);

    public static ParseError UnclosedDelimiter(Token token)
        => new ParseError($"An unclosed delimiter ({token}) was found", token.Line, token.Char);

    public static ParseError NoArgumentName(Token token)
        => new ParseError($"An argument name was expected", token.Line, token.Char);

    public ParseError(string error, int line, int charn)
    {
        Error = error;
        Line = line;
        Char = charn;
    }
}

public struct ParseResult
{
    public GoalNode Goal;
    public List<ParseError> Errors;

    public ParseResult(GoalNode goal, List<ParseError> errors)
    {
        Goal = goal;
        Errors = errors;
    }
}

// Hand-written top-down recursive descent parser
public class Parser
{
    private Tokenizer Tokenizer;
    public Parser(Tokenizer tokenizer)
    {
        Tokenizer = tokenizer;
    }

    public ParseResult Parse()
    {
        var goal = new GoalNode(); 
        var errors = new List<ParseError>();

        Token lookAhead;

        while((lookAhead = Tokenizer.Peek()).IsNot(TokenType.EOF))
        {
            if(lookAhead.Is(TokenType.Fn))
            {
                goal.Functions.Add(ParseFunctionDefinition(errors));
            }
            else
            {
                // FIXME: Just for testing purposes
                Tokenizer.Consume();
            }
        }

        return new ParseResult(goal, errors);
    }

    private FnDefNode ParseFunctionDefinition(List<ParseError> errors)
    {
        // fn name(type1 arg1, type2 arg2) -> retType { stuff }

        var function = new FnDefNode();

        var fnToken = Tokenizer.Consume(); // fn

        var idToken = Tokenizer.Peek(); // id
        if(idToken.IsNot(TokenType.Id)) 
        {  
            Token recovery;
            while((recovery = Tokenizer.Peek()).IsNot(TokenType.Id, TokenType.LParen, TokenType.LCurly, TokenType.EOF)) 
                Tokenizer.Consume();

            if(recovery.Is(TokenType.Id)) 
            {
                errors.Add(ParseError.UnexpectedToken(idToken, TokenType.Id));
                function.Id = recovery;
                Tokenizer.Consume();
            }
            else if(recovery.Is(TokenType.LParen))
            {
                errors.Add(ParseError.MissingFunctionName(fnToken));
            }
            else if(recovery.Is(TokenType.LCurly))
            {
                errors.Add(ParseError.InvalidFunctionDefinition(fnToken));
                goto ParseBlock;
            }
            else if(recovery.Is(TokenType.EOF)) 
            {
                errors.Add(ParseError.InvalidFunctionDefinition(fnToken));
                return function;
            }
        }
        else
        {
            function.Id = idToken;
            Tokenizer.Consume();
        }

        var paramsToken = Tokenizer.Peek();

        if(paramsToken.IsNot(TokenType.LParen))
        {
            errors.Add(ParseError.InvalidFunctionDefinition(fnToken));
            errors.Add(ParseError.UnexpectedToken(paramsToken, TokenType.LParen));
            while((paramsToken = Tokenizer.Peek()).IsNot(TokenType.LParen, TokenType.EOF, TokenType.LCurly)) { Tokenizer.Consume(); }
            if(paramsToken.Is(TokenType.LParen)) function.Params = ParseFunctionDefinitionParams(errors);
        }
        else
        {
            function.Params = ParseFunctionDefinitionParams(errors);
        }

        if(Tokenizer.Peek().Is(TokenType.RetArrow)) { function.ReturnType = ParseFunctionReturnValue(errors); }

ParseBlock:

        var blockToken = Tokenizer.Peek();
        while((blockToken = Tokenizer.Peek()).IsNot(TokenType.LCurly, TokenType.EOF)) Tokenizer.Consume();
        if(blockToken.Is(TokenType.LCurly)) function.Block = ParseBlock(errors);

        return function;
    }

    private Token? ParseFunctionReturnValue(List<ParseError> errors)
    {
        var pivot = Tokenizer.Consume(); // ->

        var type = Tokenizer.Peek();
        if(type.Is(TokenType.Id)) 
        {
            Tokenizer.Consume();
            return type;
        }

        errors.Add(ParseError.UnexpectedToken(type, TokenType.Id));
        while((type = Tokenizer.Peek()).IsNot(TokenType.LCurly, TokenType.EOF)) { Tokenizer.Consume(); }
        if(type.Is(TokenType.LCurly)) { errors.Add(ParseError.NoFunctionReturnType(pivot)); }

        return null;
    }

    private TypeNode ParseType(List<ParseError> errors)
    {
        TypeNode type = new();

        type.Base = Tokenizer.Consume();
        type.Indexes = 0;

        while(Tokenizer.Peek().Is(TokenType.Mul))
        {
            type.Indexes++;
            Tokenizer.Consume();
        }

        return type;
    }

    private List<FnDefParamNode> ParseFunctionDefinitionParams(List<ParseError> errors)
    {
        var args = new List<FnDefParamNode>();

        var paramsToken = Tokenizer.Consume(); // (
        Token token;

        while((token = Tokenizer.Peek()).IsNot(TokenType.RParen))
        {
            if(token.Is(TokenType.Id)) // type
            {
                var param = new FnDefParamNode();
                param.Type = ParseType(errors);

                token = Tokenizer.Peek(); // argName
                if(token.IsNot(TokenType.Id))
                {
                    errors.Add(ParseError.NoArgumentName(token));

                    if(Tokenizer.Peek().Is(TokenType.LBrack))
                    {
                        errors.Add(ParseError.InvalidArrayDefinition(param.Type.Value.Base!.Value));
                    }

                    while((token = Tokenizer.Peek()).IsNot(TokenType.Comma, TokenType.RParen, TokenType.EOF)) 
                    { 
                        if(token.Is(TokenType.Id)) param.Id = token;
                        Tokenizer.Consume(); 
                    }
                    if(token.Is(TokenType.Comma)) Tokenizer.Consume();
                    args.Add(param);
                    continue;
                }

                Tokenizer.Consume();
                param.Id = token;
                args.Add(param);

                token = Tokenizer.Peek(); // comma or rparen
                if(token.Is(TokenType.Comma, TokenType.RParen))
                {
                    if(token.Is(TokenType.Comma)) Tokenizer.Consume();
                    continue;
                }
                else
                {
                    errors.Add(ParseError.InvalidArgumentDefinition(token));
                    while((token = Tokenizer.Peek()).IsNot(TokenType.Comma, TokenType.RParen, TokenType.EOF)) { Tokenizer.Consume(); }
                    if(token.Is(TokenType.Comma)) Tokenizer.Consume();
                    continue;
                }
            }
            else if(token.Is(TokenType.EOF))
            {
                errors.Add(ParseError.UnclosedDelimiter(paramsToken));
                return args;
            }
            else
            {
                errors.Add(ParseError.InvalidArgumentDefinition(token));
                errors.Add(ParseError.UnexpectedToken(token, TokenType.Id));
                while((token = Tokenizer.Peek()).IsNot(TokenType.Id, TokenType.Comma, TokenType.RParen, TokenType.EOF)) { Tokenizer.Consume(); }
                if(token.Is(TokenType.Comma)) Tokenizer.Consume();
                continue;
            }
        }

        Tokenizer.Consume(); // )

        return args;
    }

    private BlockNode ParseBlock(List<ParseError> errors)
    {
        var block = new BlockNode();
        var begin = Tokenizer.Consume(); // {

        Token token;
        while((token = Tokenizer.Peek()).IsNot(TokenType.RCurly, TokenType.EOF))
        {
            if(token.Is(TokenType.Let)) block.Lines.Add(ParseDeclaration(errors));
            else if(token.IsValue()) block.Lines.Add(ParseExpression(errors));
            else if(token.Is(TokenType.Mut)) block.Lines.Add(ParseMutation(errors));
            else if(token.Is(TokenType.If)) { block.Lines.Add(ParseIf(errors)); continue; }
            else if(token.Is(TokenType.Else)) { block.Lines.Add(ParseElse(errors)); continue; }
            else 
            {
                errors.Add(ParseError.UnexpectedToken(token, TokenType.Let));
                Tokenizer.Consume();
                continue;
            }

            if(Tokenizer.Peek().Is(TokenType.Semi)) { Tokenizer.Consume(); }
            else if(Tokenizer.Peek().Is(TokenType.RCurly) && block.Lines.Count > 0) 
            {
                block.ReturnLast = true;
            }
            else
            {
                errors.Add(ParseError.SemicolonMissing(Tokenizer.Peek()));
            }
        }

        if(token.IsNot(TokenType.RCurly)) { errors.Add(ParseError.UnclosedDelimiter(begin)); }
        else Tokenizer.Consume();

        return block;
    }

    private IfNode ParseIf(List<ParseError> errors)
    {
        IfNode ifNode = new();
        Tokenizer.Consume(); // if
        ifNode.Condition = ParseExpression(errors);
        ifNode.Block = ParseBlock(errors);
        return ifNode;
    }

    private IBlockLineNode ParseElse(List<ParseError> errors)
    {
        Tokenizer.Consume(); // else
        if(Tokenizer.Peek().Is(TokenType.If))
        {
            ElseIfNode elseIfNode = new();
            var temp = ParseIf(errors);
            elseIfNode.Condition = temp.Condition;
            elseIfNode.Block = temp.Block;
            return elseIfNode;
        }

        ElseNode elseNode = new();
        elseNode.Block = ParseBlock(errors);
        return elseNode;
    }

    private MutationNode ParseMutation(List<ParseError> errors)
    {
        MutationNode mutation = new();
        var mutToken = Tokenizer.Consume(); // mut

        var token = Tokenizer.Peek(); // type/id
        if(token.IsNot(TokenType.Id))
        {
            while((token = Tokenizer.Peek()).IsNot(TokenType.Semi, TokenType.RCurly, TokenType.Id)) 
                Tokenizer.Consume();
            if(token.IsNot(TokenType.Id)) return mutation;
        }

        mutation.Id = token;
        Tokenizer.Consume();

        var op = Tokenizer.Peek();
        if(!op.IsMutOperator())
        {
            while((token = Tokenizer.Peek()).IsNot(TokenType.Semi, TokenType.RCurly)) 
                Tokenizer.Consume();
            return mutation;
        }

        Tokenizer.Consume();

        mutation.Expr = ParseExpression(errors);
        return mutation;
    }

    private DeclarationNode ParseDeclaration(List<ParseError> errors)
    {
        DeclarationNode declaration = new();
        var letToken = Tokenizer.Consume(); // let

        var token = Tokenizer.Peek(); // type/id
        if(token.IsNot(TokenType.Id))
        {
            errors.Add(ParseError.UnexpectedToken(token, TokenType.Id));
            errors.Add(ParseError.InvalidVarDeclaration(letToken));
            while((token = Tokenizer.Peek()).IsNot(TokenType.Semi, TokenType.RCurly, TokenType.Id)) 
                Tokenizer.Consume();
            if(token.IsNot(TokenType.Id)) return declaration;
        }

        declaration.Type = ParseType(errors);

        token = Tokenizer.Peek(); // id/=
        if(token.IsNot(TokenType.Id, TokenType.Assign))
        {
            errors.Add(ParseError.UnexpectedToken(token, TokenType.Id, TokenType.Assign));
            errors.Add(ParseError.InvalidVarDeclaration(letToken));
            while((token = Tokenizer.Peek()).IsNot(TokenType.Semi, TokenType.RCurly, TokenType.Id, TokenType.Assign)) 
                Tokenizer.Consume();
            if(token.IsNot(TokenType.Id, TokenType.Assign)) return declaration;
        }

        if(token.Is(TokenType.Id))
        {
            declaration.Id = token;
            Tokenizer.Consume();
        }
        else
        {
            declaration.Id = declaration.Type.Value.Base;
            declaration.Type = null;
        }

        token = Tokenizer.Peek();
        if(token.IsNot(TokenType.Assign))
        {
            errors.Add(ParseError.UnexpectedToken(token, TokenType.Assign));
            errors.Add(ParseError.InvalidVarDeclaration(letToken));
            while((token = Tokenizer.Peek()).IsNot(TokenType.Semi, TokenType.RCurly, TokenType.Assign)) 
                Tokenizer.Consume();
            if(token.IsNot(TokenType.Assign)) return declaration;
        }

        Tokenizer.Consume(); // =

        declaration.Expr = ParseExpression(errors);
        return declaration;
    }

    private IExpression ParseExpression(List<ParseError> errors, int minPrecedence = int.MinValue)
    {
        IExpression expr;
        var lhs = ParseExpressionLeaf(errors);

        while(true)
        {
            expr = ParseExpressionIncreasingPrec(errors, lhs, minPrecedence);
            if(expr == lhs) return expr;
            lhs = expr;
        }
    }

    private IExpression ParseExpressionIncreasingPrec(List<ParseError> errors, IExpression lhs, int minPrecedence)
    {
        if(!Tokenizer.Peek().IsBinaryOperator()) return lhs;
        
        var opToken = Tokenizer.Consume();
        var precedence = opToken.GetBinaryOperatorPrecedence();
        if(precedence <= minPrecedence)
        {
            return lhs;
        }
        else
        {
            OpExprNode op = new();
            op.Operator = opToken;
            op.Lhs = lhs;
            op.Rhs = ParseExpression(errors, precedence);
            return op;
        }
    }

    private IExpression ParseExpressionLeaf(List<ParseError> errors)
    {
        var token = Tokenizer.Peek();

        if(token.Is(TokenType.LCurly)) return ParseBlock(errors);
        else if(token.Is(TokenType.Id, TokenType.Number, TokenType.String, TokenType.True, TokenType.False))
            return ParseChainLiteral(errors);
        else if(token.Is(TokenType.Not)) return ParseUnaryOperator(errors);

        errors.Add(ParseError.UnexpectedToken(token, TokenType.LCurly, TokenType.Not, TokenType.Id, TokenType.Number, TokenType.String, TokenType.True, TokenType.False));

        while((token = Tokenizer.Peek()).IsNot(TokenType.LCurly, TokenType.Not, TokenType.Id, TokenType.Number, TokenType.String, TokenType.True, TokenType.False, TokenType.RCurly, TokenType.EOF, TokenType.Semi))
            Tokenizer.Consume();

        if(token.Is(TokenType.RCurly, TokenType.EOF, TokenType.Semi)) return new BlockNode();
        return ParseExpressionLeaf(errors);
    }

    private UnOpExtrNode ParseUnaryOperator(List<ParseError> errors)
    {
        UnOpExtrNode un = new();

        un.Operator = Tokenizer.Consume();
        un.Expr = ParseExpressionLeaf(errors);

        return un;
    }

    private ChainLitExprNode ParseChainLiteral(List<ParseError> errors)
    {
        ChainLitExprNode chain = new();

        var token = Tokenizer.Peek();
        while((token = Tokenizer.Peek()).Is(TokenType.Id, TokenType.Number, TokenType.String, TokenType.True, TokenType.False))
        {
            chain.Chain.Add(ParseSingleLiteral(errors));

            if(Tokenizer.Peek().Is(TokenType.Dot)) { Tokenizer.Consume(); }
            else break;
        }

        return chain;
    }

    private ILiteral ParseSingleLiteral(List<ParseError> errors)
    {
        var origin = Tokenizer.Consume();
        var next = Tokenizer.Peek();

        if(next.Is(TokenType.LParen))
        {
            Tokenizer.Consume();
            FuncExprNode func = new();

            while((next = Tokenizer.Peek()).IsNot(TokenType.RParen, TokenType.EOF))
            {
                func.Args.Add(ParseExpression(errors));

                if(Tokenizer.Peek().Is(TokenType.Comma)) Tokenizer.Consume();
                else if(Tokenizer.Peek().Is(TokenType.RParen)) {}
                else
                {
                    errors.Add(ParseError.UnexpectedToken(Tokenizer.Peek(), TokenType.Comma, TokenType.RParen));
                    while((next = Tokenizer.Peek()).IsNot(TokenType.Comma, TokenType.RParen, TokenType.EOF)) Tokenizer.Consume();
                    if(Tokenizer.Peek().Is(TokenType.Comma)) Tokenizer.Consume();
                }
            }

            if(next.IsNot(TokenType.RParen))
            {
                errors.Add(ParseError.UnexpectedToken(next, TokenType.RParen));
            }
            else
            {
                Tokenizer.Consume();
            }

            return func;
        }
        else if(next.Is(TokenType.LBrack))
        {
            Tokenizer.Consume();
            ArrayExprNode array = new();

            LiteralExprNode arrayOrigin = new();
            arrayOrigin.Literal = origin;

            array.Array = arrayOrigin;
            array.Index = ParseExpression(errors);

            if(Tokenizer.Peek().Is(TokenType.RBrack))
            {
                Tokenizer.Consume();
                return array;
            }

            errors.Add(ParseError.UnexpectedToken(Tokenizer.Peek(), TokenType.RBrack));
            while((next = Tokenizer.Peek()).IsNot(TokenType.RBrack, TokenType.RCurly, TokenType.Semi, TokenType.EOF))
                Tokenizer.Consume();
            if(next.Is(TokenType.RBrack)) Tokenizer.Consume();
            return array;
        }
        else 
        {
            var literal = new LiteralExprNode();
            literal.Literal = origin;
            return literal;
        }
    }
}
