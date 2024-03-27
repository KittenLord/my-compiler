using System;
using System.Linq;
using System.Collections.Generic;
using MyCompiler.Parsing;

namespace MyCompiler;

public struct AttachedMessage
{
    public string Title;
    public string Message;

    public Position Position; 

    public AttachedMessage(string title, string msg, Position position)
    {
        Title = title;
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

    public void Error(string title, Position position, params object[] args) 
        => Error(title, "", position, args);

    public void Error(string title, string message, Position position, params object[] args)
    {
        Success = false;
        var msg = string.Format(message, args);
        Errors.Add(new AttachedMessage(title, msg, position));
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
            Error("", "", Peek().Position);
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
                Error("", "", Peek().Position);
                goto Arguments;
            }
            else
            {
                // Invalid function definition
                Error("", "", Peek().Position);
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
                Error("", "", Peek().Position);
                goto Block;
            }
            else
            {
                // Invalid function definition
                Error("", "", Peek().Position);
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
            Error("", "", Peek().Position);
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
            Error("", "", Peek().Position);
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
            Error("", "", Peek().Position);
        }

        return fn;
    }

    private TupleNode ParseTuple()
    {
        TupleNode tuple = new();
        Consume();

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
                Error("", "", Peek().Position);
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
                Error("", "", Peek().Position);
                var recovery = ConsumeUntil(TokenType.RParen, TokenType.Id);
                if(recovery.IsEnd()) continue;
                if(recovery.Is(TokenType.RParen)) continue;
                goto Name;
            }

End:
            tuple.Elements.Add(element);

            if(Peek().Is(TokenType.Comma)) { Consume(); continue; }
            if(Peek().Is(TokenType.RParen)) { continue; }

            Error("", "", Peek().Position);
        }

        if(Consume().Is(TokenType.EOF))
        {
            // Tuple definition is not closed
            Error("", "", Peek().Position);
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
        Consume();

        while(Peek().IsNot(TokenType.RCurly, TokenType.EOF))
        {
            if(Peek().Is(TokenType.Let)) { block.Lines.Add(ParseLet()); }
            else if(Peek().CanStartExpression()) { block.Lines.Add(ParseExpression()); }

            if(Peek().Is(TokenType.RCurly)) { block.ReturnLast = true; continue; }
            if(Peek().Is(TokenType.Semi)) { Consume(); continue; }

            Error("", "", Peek().Position);
        }

        if(Peek().Is(TokenType.RCurly)) { Consume(); }
        else if(Peek().IsEnd()) { Error("", "", Peek().Position); }

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

            }
        }

Assign:

        if(Peek().Is(TokenType.Assign)) 
        {
            Consume();
        }
        else
        {

        }

Expression:

        if(Peek().CanStartExpression())
        {
            let.Expression = ParseExpression();
        }
        else
        {

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
                    Error("", "", Peek().Position);
                    continue;
                }
            }
        }

        return leaf;
    }
}
