using System;
using System.Linq;
using System.Collections.Generic;
using MyCompiler.Parsing;

namespace MyCompiler;

public struct AttachedMessage
{
    public string Title;
    public string Message;

    public int Line;
    public int Char;

    public AttachedMessage(string title, string msg, int line, int charn)
    {
        Title = title;
        Message = msg;
        Line = line;
        Char = charn;
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

    private Dictionary<TokenType, TokenType> delimiters = new() {
        { TokenType.LParen, TokenType.RParen },
        { TokenType.LCurly, TokenType.RCurly },
        { TokenType.LBrack, TokenType.RBrack }
    };

    public void Error(string title, Token position, params object[] args) 
        => Error(title, "", position, args);

    public void Error(string title, string message, Token position, params object[] args)
    {
        var msg = string.Format(message, args);
        Errors.Add(new AttachedMessage(title, msg, position.Line, position.Char));
    }

    private Token Peek() { return Tokenizer.Peek(); }
    private Token Peek(out Token token) { token = Tokenizer.Peek(); return token; }

    private Token Consume() { return Tokenizer.Consume(); }
    private Token Consume(out Token token) { token = Tokenizer.Consume(); return token; }

    private Token ConsumeUntil(params TokenType[] types)
    {
        // This handles closures, since the blocks can end without a semi
        // we expect a } token, but in a case of something like:
        //
        //      fn a() {
        //          let a = 0 0 {>>}<<
        //          ...
        //      >>}<<
        //
        // we don't wont to stop parsing the fn after the first }, rather
        // after the second }

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
}
