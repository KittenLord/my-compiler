using System;
using System.Linq;
using System.Collections.Generic;
using MyCompiler.Parsing;

namespace MyCompiler;

public static class ParseError
{
    public static string UnexpectedToken(Token token, params TokenType[] expected)
        => $@"Unexpected token {token}
    Expected: [{string.Join(", ", expected)}]";

    public const string MissingFunctionName 
        = "Function definition is missing a name";

    public const string MissingFunctionArguments
        = "Function definition is missing its arguments";

    public const string InvalidFunctionDeclaration
        = @"Function declaration is invalid. Declare a function like this:
    fn NAME(TYPE1 ARG1, TYPE2 ARG2, ...) -> RETURN_TYPE { ... }";

    public const string InvalidVariableDeclaration
        = @"Variable declaration is invalid. Declare a variable like this:
    let [TYPE] NAME = ...;";

    public static string ExpectedType(Token token)
        => @$"Expected a type, found: {token}";

    public static string ExpectedExpression(Token token)
        => @$"Expected an expression, found: {token}";

    public static string UnclosedDelimiter(Token token)
        => @$"Unclosed delimiter: {token}";

    public const string MissingSemicolon
        = @"Missing a semicolon. Only the last line of a block can be implicitly returned without a semicolon";

    public static string UnexpectedLineBegin(Token token)
        => @$"A block line can't start with token {token}. Did you miss an operator (+ - * /) ?";

    public const string MissingVariableName
        = "Variable declaration is missing its name";
}

public struct AttachedMessage
{
    public string Message;
    public Position Position; 

    public override string ToString()
        => $"{Message}\nat {Position}";

    public AttachedMessage(string msg, Position position)
    {
        Message = msg;
        Position = position;
    }
}

// Hand-written top-down recursive descent parser
public class Parser
{
    public bool Success = true;
    public List<AttachedMessage> Errors;

    private Tokenizer Tokenizer;
    public Parser(Tokenizer tokenizer)
    {
        Tokenizer = tokenizer;
        Errors = new();
    }

    private void Error(string message, Token token)
        => Error(message, token.Position);

    private void Error(string message, Position position)
    {
        Success = false;
        Errors.Add(new AttachedMessage(message, position));
    }

    private Token Peek() { return Tokenizer.Peek(); }
    private Token Peek(out Token token) { token = Tokenizer.Peek(); return token; }

    private Token Consume() { return Tokenizer.Consume(); }
    private Token Consume(out Token token) { token = Tokenizer.Consume(); return token; }

    private readonly Dictionary<TokenType, TokenType> delimiters = new() {
        { TokenType.LParen, TokenType.RParen },
        { TokenType.LCurly, TokenType.RCurly },
        { TokenType.LBrack, TokenType.RBrack }
    };
    private Token ConsumeUntil(params TokenType[] types)
    {
        // This handles closures, since the blocks can end without a semi
        // we need to expect a RCurly token, but in a case of something like this:
        //
        //      fn a() {
        //          let a = 0 0 {>>}<<
        //          ...
        //      >>}<<
        //
        // we don't want to stop parsing the fn after the second RCurly, rather
        // after the first RCurly
        // Though this has a great possibility to fuck up the entire parse
        // Probably need to read until: EOF, Semi, RCurly, Struct, Fn, since
        // there will definitely be no nested functions and structs

        var closures = delimiters.ToDictionary(kv => kv.Value, kv => 0);
        while(true)
        {
            var token = Peek();

            if(delimiters.ContainsKey(token.Type)) closures[delimiters[token.Type]]++;
            if(token.IsNot(types) && token.IsNot(TokenType.EOF)) { Consume(); continue; }

            if(!closures.ContainsKey(token.Type)) return token;
            if(closures[token.Type] > 0) { closures[token.Type]--; Consume(); continue; }
            return token;
        }
    }

    private Token ConsumeUntilRaw(params TokenType[] types)
    {
        while(true)
        {
            var token = Peek();
            if(token.Is(types) || token.Is(TokenType.EOF)) return token;
            Consume();
        }
    }

    public ParseTree Parse()
    {
        ParseTree tree = new();

        while(!Peek().IsEnd())
        {
            if(Peek().Is(TokenType.Fn))
            {
                tree.Functions.Add(ParseFunctionDefinition());
                continue;
            }

            // Shit
            Error(ParseError.UnexpectedToken(Peek(), TokenType.Fn, TokenType.Type), Peek());
            Consume();
        }

        return tree;
    }

    private FunctionDefinitionNode ParseFunctionDefinition()
    {
        FunctionDefinitionNode fn = new(Consume().Position);

Name:

        if(Peek().Is(TokenType.Id))
        {
            fn.Name = Consume().Value;
            goto Arguments;
        }
        else
        {
            if(Peek().Is(TokenType.LParen))
            {
                // Missing function name
                Error(ParseError.MissingFunctionName, Peek());
                goto Arguments;
            }
            else
            {
                // Invalid function definition
                Error(ParseError.InvalidFunctionDeclaration, fn.Position);
                var recovery = ConsumeUntil(
                        (fn.Name is null ? TokenType.Id : TokenType.EOF), 
                        TokenType.LParen, 
                        TokenType.LCurly);

                if(recovery.IsEnd()) return fn;
                if(recovery.Is(TokenType.Id)) goto Name;
                if(recovery.Is(TokenType.LParen)) goto Arguments;
                if(recovery.Is(TokenType.LCurly)) goto Block;
            }
        }

Arguments:

        if(Peek().Is(TokenType.LParen))
        {
            fn.Arguments = ParseTuple().Elements;
            goto ReturnValue;
        }
        else
        {
            if(Peek().Is(TokenType.LCurly))
            {
                // Missing arguments
                Error(ParseError.MissingFunctionArguments, fn.Position);
                goto Block;
            }
            else
            {
                // Invalid function definition
                Error(ParseError.InvalidFunctionDeclaration, fn.Position);
                var recovery = ConsumeUntil(
                        TokenType.LParen, 
                        TokenType.LCurly);

                if(recovery.IsEnd()) return fn;
                if(recovery.Is(TokenType.LParen)) goto Arguments;
                if(recovery.Is(TokenType.LCurly)) goto Block;
            }
        }

ReturnValue:

        if(Peek().Is(TokenType.LCurly)) goto Block;
        if(Peek().IsNot(TokenType.RetArrow))
        {
            // Invalid function definition
            Error(ParseError.InvalidFunctionDeclaration, fn.Position);
            var recovery = ConsumeUntil(TokenType.RetArrow, TokenType.LCurly);

            if(recovery.IsEnd()) return fn;
            if(recovery.Is(TokenType.RetArrow)) goto ReturnValue;
            if(recovery.Is(TokenType.LCurly)) goto Block;
        }

        Consume();
        if(Peek().Is(TokenType.Id))
        {
            fn.ReturnType = ParseType();
            goto Block;
        }
        else
        {
            // Invalid function definition
            Error(ParseError.InvalidFunctionDeclaration, fn.Position);
            var recovery = ConsumeUntil(TokenType.Id, TokenType.LCurly);
            if(recovery.IsEnd()) return fn;
            if(recovery.Is(TokenType.Id)) { fn.ReturnType = ParseType(); goto Block; }
            if(recovery.Is(TokenType.LCurly)) goto Block;
        }

Block:

        if(Peek().Is(TokenType.LCurly))
        {
            fn.Block = ParseBlock();
        }
        else
        {
            // Invalid function definition
            Error(ParseError.InvalidFunctionDeclaration, fn.Position);
            var recovery = ConsumeUntil(TokenType.LCurly);
            if(recovery.IsEnd()) return fn;
            if(recovery.Is(TokenType.LCurly)) goto Block;
        }

        return fn;
    }

    private TupleNode ParseTuple()
    {
        TupleNode tuple = new();
        var delimiter = Consume();

        while(Peek().IsNot(TokenType.RParen, TokenType.EOF))
        {
            var element = new VariableNode();

Type:

            if(Peek().Is(TokenType.Id))
            {
                element.Type = ParseType();
                goto Name;
            }
            else
            {
                Error(ParseError.ExpectedType(Peek()), Peek());
                var recovery = ConsumeUntil(TokenType.RParen, TokenType.Id);
                if(recovery.IsEnd()) continue;
                if(recovery.Is(TokenType.RParen)) continue;
                goto Type;
            }

Name:
            if(Peek().Is(TokenType.Id))
            {
                element.Name = Consume().Value;
                goto End;
            }
            else
            {
                Error(ParseError.UnexpectedToken(Peek(), TokenType.Id), Peek());
                var recovery = ConsumeUntil(TokenType.RParen, TokenType.Id);
                if(recovery.IsEnd()) continue;
                if(recovery.Is(TokenType.RParen)) continue;
                goto Name;
            }

End:
            tuple.Elements.Add(element);

            if(Peek().Is(TokenType.Comma)) { Consume(); continue; }
            if(Peek().Is(TokenType.RParen)) { continue; }

            Error(ParseError.UnexpectedToken(Peek(), TokenType.Comma, TokenType.RParen), Peek());

            var rec = ConsumeUntil(TokenType.Comma, TokenType.RParen);
            if(rec.IsEnd()) return tuple;
            if(rec.Is(TokenType.Comma)) Consume();
        }

        if(Consume().Is(TokenType.EOF))
        {
            // Tuple definition is not closed
            Error(ParseError.UnclosedDelimiter(delimiter), Peek());
        }

        return tuple;
    }

    private ITypeNode ParseType()
    {
        ITypeNode type = new TypeNode(Consume().Value);

        while(Peek(out var token).Is(TokenType.Mul, TokenType.Pointer))
        {
            if(token.Is(TokenType.Mul)) type = new TypeArrayNode(type);
            if(token.Is(TokenType.Pointer)) type = new TypePointerNode(type);
            Consume();
        }

        return type;
    }

    private BlockNode ParseBlock()
    {
        BlockNode block = new();
        var delimiter = Consume();

        while(Peek().IsNot(TokenType.RCurly, TokenType.EOF))
        {
            if(Peek().Is(TokenType.Let)) { block.Lines.Add(ParseLet()); }
            else if(Peek().CanStartExpression()) { block.Lines.Add(ParseExpression()); }
            else
            {
                Error(ParseError.UnexpectedLineBegin(Peek()), Peek());
                while(!Peek().CanStartLine() && Peek().IsNot(TokenType.EOF, TokenType.Semi, TokenType.RCurly))
                    Consume();
            }

            if(Peek().Is(TokenType.RCurly)) { block.ReturnLast = true; continue; }
            if(Peek().Is(TokenType.Semi)) { Consume(); continue; }

            Error(ParseError.MissingSemicolon, Peek());
        }

        if(Peek().Is(TokenType.RCurly)) { Consume(); }
        else if(Peek().IsEnd()) 
        { 
            Error(ParseError.UnclosedDelimiter(delimiter), Peek()); 
        }

        return block;
    }

    private LetDefinitionNode ParseLet()
    {
        LetDefinitionNode let = new();
        Consume();

Type:

        if(Peek().Is(TokenType.Id))
        {
            let.Type = ParseType();
            goto Name;
        }
        else
        {
            Error(ParseError.ExpectedType(Peek()), Peek());
            while(Peek().IsNot(TokenType.EOF, TokenType.Id, TokenType.Assign, TokenType.Semi, TokenType.RCurly))
                Consume();
            if(Peek().Is(TokenType.Id)) goto Type;
            if(Peek().Is(TokenType.Assign)) goto Assign;
            return let;
        }

Name:

        if(Peek().Is(TokenType.Id))
        {
            let.Name = Consume().Value;
            goto Assign;
        }
        else if(Peek().Is(TokenType.Assign))
        {
            if(let.Type is TypeNode type)
            {
                let.Name = type.Name;
                let.Type = new TypeNoneNode();
                goto Assign;
            }
            else
            {
                Error(ParseError.MissingVariableName, Peek());
            }
        }
        else
        {
            Error(ParseError.InvalidVariableDeclaration, Peek());
            var recovery = ConsumeUntil(TokenType.Id, TokenType.Assign, TokenType.Semi, TokenType.RCurly);
            if(recovery.Is(TokenType.Id, TokenType.Assign)) goto Name;
            return let;
        }

Assign:

        if(Peek().Is(TokenType.Assign)) 
        {
            Consume();
        }
        else
        {
            Error(ParseError.InvalidVariableDeclaration, Peek());
            var recovery = ConsumeUntil(TokenType.Assign, TokenType.Semi, TokenType.RCurly);
            if(recovery.Is(TokenType.Assign)) goto Assign;
            return let;
        }

Expression:

        if(Peek().CanStartExpression())
        {
            let.Expression = ParseExpression();
        }
        else
        {
            Error(ParseError.ExpectedExpression(Peek()), Peek());
            while(!Peek().CanStartExpression() && Peek().IsNot(TokenType.Semi, TokenType.RCurly, TokenType.EOF))
                Consume();
            if(Peek().CanStartExpression()) goto Expression;
            return let;
        }

        return let;
    }

    private IExpressionNode ParseExpression(int precedence = int.MinValue)
    {
        IExpressionNode expression;
        var lhs = ParseLeaf();

        while(true)
        {
            expression = ParseExpressionIncreasingPrecedence(lhs, precedence);
            if(expression == lhs) return expression;
            lhs = expression;
        }
    }

    private IExpressionNode ParseExpressionIncreasingPrecedence(IExpressionNode expr, int precedence)
    {
        if(!Peek().IsBinaryOperator()) return expr;

        var p = Peek().GetBinaryOperatorPrecedence();
        if(p < precedence) return expr;
        else return new OperatorExpressionNode(Consume(), expr, ParseExpression(p));
    }

    private IExpressionNode ParseLeaf()
    {
        IAccessible leaf;

        if(Peek().Is(TokenType.LCurly)) 
            { leaf = ParseBlock(); }
        else if(Peek().Is(TokenType.Number)) 
            { leaf = new LiteralExpressionNode(new NumberLiteralNode(Consume().Value)); }
        else if(Peek().Is(TokenType.String))
            { leaf = new LiteralExpressionNode(new StringLiteralValue(Consume().Value)); }
        else if(Peek().Is(TokenType.True, TokenType.False))
            { leaf = new LiteralExpressionNode(new BoolLiteralNode(Consume().Is(TokenType.True))); }
        else if(Peek().Is(TokenType.Id))
            { leaf = new LiteralExpressionNode(new IdLiteralNode(Consume().Value)); }
        // NOTE: If any exceptions fire, the compiler needs to be fixed
        else throw new Exception(); 

        while(Peek(out var next).Is(TokenType.Pointer, TokenType.LParen, TokenType.LBrack, TokenType.Dot))
        {
            if(next.Is(TokenType.Pointer)) { leaf = new PointerAccessorNode(leaf); Consume(); }
            else if(next.Is(TokenType.Dot))
            {
                Consume();
                if(Peek().Is(TokenType.Id))
                {
                    leaf = new MemberAccessorNode(leaf, Consume().Value);
                    continue;
                }
                else
                {
                    Error(ParseError.UnexpectedToken(Peek(), TokenType.Id), Peek());
                    while(Peek().Is(
                                TokenType.Id,
                                TokenType.RCurly, TokenType.Semi, TokenType.EOF,
                                TokenType.Pointer, TokenType.LParen, TokenType.LBrack, TokenType.Dot))
                        Consume();
                    if(Peek().Is(TokenType.Id)) leaf = new MemberAccessorNode(leaf, Consume().Value);
                    else if(Peek().Is(TokenType.RCurly, TokenType.Semi, TokenType.EOF)) return leaf;
                    continue;
                }
            }
            else if(next.Is(TokenType.LBrack))
            {
                Consume();
                if(Peek().CanStartExpression())
                {
                    leaf = new ArrayAccessorNode(leaf, ParseExpression());

                    if(Peek().Is(TokenType.RBrack)) { Consume(); continue; }
                    else
                    {
                        Error(ParseError.UnexpectedToken(Peek(), TokenType.RBrack), Peek());
                        var recovery = ConsumeUntil(TokenType.RBrack, TokenType.RCurly, TokenType.Semi);
                        if(recovery.Is(TokenType.RBrack)) { Consume(); }
                        continue;
                    }
                }
                else
                {
                    Error(ParseError.ExpectedExpression(Peek()), Peek());
                    while(!Peek().CanStartExpression() && 
                           Peek().IsNot(TokenType.EOF, TokenType.Semi, TokenType.RCurly,
                                        TokenType.RBrack))
                        Consume();
                    if(Peek().Is(TokenType.RBrack)) continue;
                    return leaf;
                }
            }
            else if(next.Is(TokenType.LParen))
            {
                var func = new FuncAccessorNode(leaf);
                var delimiter = Consume();

                while(Peek().IsNot(TokenType.RParen, TokenType.EOF))
                {
                    if(Peek().CanStartExpression())
                    {
                        func.Arguments.Add(ParseExpression());

                        if(Peek().Is(TokenType.Comma)) { Consume(); continue; }
                        if(Peek().Is(TokenType.RParen)) { continue; } 

                        Error(ParseError.UnexpectedToken(Peek(), TokenType.Comma, TokenType.RParen), Peek());
                        var recovery = ConsumeUntil(TokenType.Comma, TokenType.RParen,
                                                    TokenType.EOF, TokenType.RCurly, TokenType.Semi);
                        if(recovery.Is(TokenType.Comma)) { Consume(); continue; }
                        if(recovery.Is(TokenType.RParen)) { continue; }
                        return leaf;
                    }
                    else
                    {
                        Error(ParseError.ExpectedExpression(Peek()), Peek());
                        while(!Peek().CanStartExpression() && 
                               Peek().IsNot(TokenType.Semi, TokenType.RCurly, TokenType.EOF,
                                            TokenType.Comma, TokenType.RParen))
                            Consume();
                        // FIXME: If I will introduce unit type, put it here
                        if(Peek().Is(TokenType.Comma)) 
                            { func.Arguments.Add(new LiteralExpressionNode(new BoolLiteralNode(true))); Consume(); continue; }
                        if(Peek().Is(TokenType.RParen)) 
                            { func.Arguments.Add(new LiteralExpressionNode(new BoolLiteralNode(true))); continue; }
                    }
                }

                if(Peek().Is(TokenType.RParen)) { Consume(); }
                else 
                {
                    Error(ParseError.UnclosedDelimiter(delimiter), Peek());
                }

                leaf = func;
                continue;
            }
        }

        return leaf;
    }
}
