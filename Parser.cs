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
    {
        return $"{Error}\n\tat [ {Line} : {Char} ]";
    }

    public static ParseError InvalidFunctionDefinition(Token token)
    {
        return new ParseError($"Invalid function definition. Functions are defined like this:\n\tfn name(type1 arg1, type2 arg2) -> retType {{ ... }}", token.Line, token.Char);
    }

    public static ParseError NoFunctionReturnType(Token token)
    {
        return new ParseError($"Function definition has -> operator, but no return type specified", token.Line, token.Char);
    }

    public static ParseError InvalidArgumentDefinition(Token token)
    {
        return new ParseError($"Invalid argument definition", token.Line, token.Char);
    }

    public static ParseError UnexpectedToken(Token token, params TokenType[] expected)
    {
        return new ParseError($"Unexpected token {{{token.Type}}} (expected: [{string.Join(", ", expected)}])", token.Line, token.Char);
    }

    public static ParseError MissingFunctionName(Token token)
    {
        return new ParseError($"Missing function name", token.Line, token.Char);
    }

    public static ParseError UnclosedDelimiter(Token token)
    {
        return new ParseError($"An unclosed delimiter ({token}) was found", token.Line, token.Char);
    }

    public static ParseError NoArgumentName(Token token)
    {
        return new ParseError($"An argument name was expected", token.Line, token.Char);
    }

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

    private List<FnDefParamNode> ParseFunctionDefinitionParams(List<ParseError> errors)
    {
        var args = new List<FnDefParamNode>();

        var paramsToken = Tokenizer.Consume(); // (
        Token token;

        while((token = Tokenizer.Consume()).IsNot(TokenType.RParen))
        {
            if(token.Is(TokenType.Id)) // type
            {
                var param = new FnDefParamNode();
                param.Type = token;

                token = Tokenizer.Peek(); // argName
                if(token.IsNot(TokenType.Id))
                {
                    errors.Add(ParseError.NoArgumentName(token));
                    while((token = Tokenizer.Peek()).IsNot(TokenType.Comma, TokenType.RParen, TokenType.EOF)) { Tokenizer.Consume(); }
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

        return args;
    }

    private FnBlock ParseBlock(List<ParseError> errors)
    {
        var block = new FnBlock();
        var begin = Tokenizer.Consume(); // {

        Token token;
        while((token = Tokenizer.Consume()).IsNot(TokenType.RCurly))
        {

        }

        return block;
    }
}
