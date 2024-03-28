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

    public const string MissingTypeName 
        = "Type definition is missing a name";

    public const string MissingFunctionArguments
        = "Function definition is missing its arguments";

    public const string InvalidFunctionDeclaration
        = @"Function declaration is invalid. Declare a function like this:
    fn NAME(TYPE1 ARG1, TYPE2 ARG2, ...) -> RETURN_TYPE { ... }";

    public const string InvalidTypeDeclaration
        = @"Type declaration is invalid. Declare a type like this:
    type NAME { TYPE1 MEMBER1; TYPE2 MEMBER2; ... }";

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

    public const string ExpectedMutableOperator
        = @"Expected a mutation operator (= += -= *= /=)";
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
            if(Peek().Is(TokenType.Type))
            {
                tree.Types.Add(ParseTypeDefinition());
                continue;
            }
            if(Peek().Is(TokenType.Let))
            {
                tree.Variables.Add(ParseLet());

                if(Peek().Is(TokenType.Semi)) { Consume(); continue; }
                Error(ParseError.UnexpectedToken(Peek(), TokenType.Semi), Peek());
                var recovery = ConsumeUntil(TokenType.Semi, TokenType.Type, TokenType.Fn, TokenType.Let);
                if(recovery.Is(TokenType.Semi)) Consume();
                continue;
            }

            Error(ParseError.UnexpectedToken(Peek(), TokenType.Fn, TokenType.Type, TokenType.Let), Peek());
            ConsumeUntil(TokenType.Fn, TokenType.Type, TokenType.Let);
        }

        return tree;
    }

    private TypeDefinitionNode ParseTypeDefinition()
    {
        TypeDefinitionNode type = new();
        var delimiter = Consume();

Name:

        if(Peek().Is(TokenType.Id))
        {
            type.Name = Consume().Value;
            goto Members;
        }
        else
        {
            if(Peek().Is(TokenType.LCurly))
            {
                Error(ParseError.MissingTypeName, Peek());
                goto Members;
            }
            else
            {
                Error(ParseError.InvalidTypeDeclaration, Peek());
                var recovery = ConsumeUntil(TokenType.Id, TokenType.LCurly, TokenType.Type, TokenType.Fn);
                if(recovery.Is(TokenType.Id)) goto Name;
                if(recovery.Is(TokenType.LCurly)) goto Members;
                return type;
            }
        }

Members:

        Consume();
        while(Peek().IsNot(TokenType.EOF, TokenType.RCurly))
        {
            var member = new VariableNode();

Type:

            if(Peek().Is(TokenType.Id))
            {
                member.Type = ParseType();
                goto MName;
            }
            else
            {
                Error(ParseError.ExpectedType(Peek()), Peek());
                var recovery = ConsumeUntil(TokenType.Id, TokenType.Semi, TokenType.RCurly);
                if(recovery.Is(TokenType.Id)) goto Type;
                if(recovery.Is(TokenType.Semi)) { Consume(); continue; };
                return type;
            }

MName:

            if(Peek().Is(TokenType.Id))
            {
                member.Name = Consume().Value;
                goto Semi;
            }
            else
            {
                Error(ParseError.UnexpectedToken(Peek(), TokenType.Id), Peek());
                var recovery = ConsumeUntil(TokenType.Id, TokenType.Semi, TokenType.RCurly);
                if(recovery.Is(TokenType.Id)) goto MName;
                if(recovery.Is(TokenType.Semi)) { Consume(); continue; }
                return type;
            }

Semi:

            type.Members.Add(member);
            if(Peek().Is(TokenType.Semi))
            {
                Consume();
            }
            else
            {
                Error(ParseError.MissingSemicolon, Peek());
                var recovery = ConsumeUntil(TokenType.Id, TokenType.RCurly);
                if(recovery.Is(TokenType.Semi)) { Consume(); continue; }
                return type;
            }
        }

        if(Peek().Is(TokenType.RCurly)) Consume();
        else
        {
            Error(ParseError.UnclosedDelimiter(delimiter), Peek());
        }

        return type;
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
            if(Peek().CanStartExpression()) { block.Lines.Add(ParseExpression()); }
            else if(Peek().Is(TokenType.Let)) { block.Lines.Add(ParseLet()); }
            else if(Peek().Is(TokenType.Mut)) { block.Lines.Add(ParseMut()); }
            else if(Peek().Is(TokenType.If)) { block.Lines.Add(ParseIf()); continue; }
            else if(Peek().Is(TokenType.Else)) { block.Lines.Add(ParseElse()); continue; }
            else if(Peek().Is(TokenType.While)) { block.Lines.Add(ParseWhile()); continue; }
            else if(Peek().Is(TokenType.Do)) { block.Lines.Add(ParseDoWhile()); continue; }
            else
            {
                Error(ParseError.UnexpectedToken(Peek(), TokenType.Let, TokenType.Mut, TokenType.If, TokenType.Else, TokenType.While, TokenType.Do, TokenType.Id, TokenType.LCurly), Peek());
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

    private WhileNode ParseDoWhile()
    {
        Consume();

        if(Peek().Is(TokenType.While))
        {
            var whileNode = ParseWhile();
            whileNode.Do = true;
            return whileNode;
        }

        Error(ParseError.UnexpectedToken(Peek(), TokenType.While), Peek());
        var recovery = ConsumeUntilRaw(TokenType.RCurly, TokenType.Semi);
        return new WhileNode();
    }

    private WhileNode ParseWhile()
    {
        WhileNode whileNode = new();
        whileNode.Block = new BlockNode();
        whileNode.Condition = new BlockNode();
        whileNode.Do = false;
        Consume();

Condition:

        if(Peek().CanStartExpression())
        {
            whileNode.Condition = ParseExpression();
            goto Block;
        }
        else
        {
            Error(ParseError.ExpectedExpression(Peek()), Peek());
            while(!Peek().CanStartExpression() && 
                  Peek().IsNot(TokenType.LCurly, TokenType.EOF, TokenType.Semi))
                Consume();
            if(Peek().Is(TokenType.LCurly)) goto Block;
            if(Peek().CanStartExpression()) goto Condition;
            return whileNode;
        }

Block:

        if(Peek().Is(TokenType.LCurly))
        {
            whileNode.Block = ParseBlock();
            return whileNode;
        }
        else
        {
            Error(ParseError.UnexpectedToken(Peek(), TokenType.LCurly), Peek());
            var recovery = ConsumeUntil(TokenType.LCurly, TokenType.Semi);
            if(Peek().Is(TokenType.LCurly)) goto Block;
            return whileNode;
        }

        return whileNode;
    }

    private IfNode ParseIf()
    {
        IfNode ifNode = new();
        Consume();

Condition:

        if(Peek().CanStartExpression())
        {
            ifNode.Condition = ParseExpression();
            goto Block;
        }
        else
        {
            Error(ParseError.ExpectedExpression(Peek()), Peek());
            while(!Peek().CanStartExpression() && 
                  Peek().IsNot(TokenType.LCurly, TokenType.EOF, TokenType.Semi))
                Consume();
            if(Peek().Is(TokenType.LCurly)) goto Block;
            if(Peek().CanStartExpression()) goto Condition;
            return ifNode;
        }

Block:

        if(Peek().Is(TokenType.LCurly))
        {
            ifNode.Block = ParseBlock();
            return ifNode;
        }
        else
        {
            Error(ParseError.UnexpectedToken(Peek(), TokenType.LCurly), Peek());
            var recovery = ConsumeUntil(TokenType.LCurly, TokenType.Semi);
            if(Peek().Is(TokenType.LCurly)) goto Block;
            return ifNode;
        }

        return ifNode;
    }

    public IBlockLineNode ParseElse()
    {
        Consume();

Begin:

        if(Peek().Is(TokenType.If)) 
        { 
            ElseIfNode elifNode = new(); 
            var result = ParseIf();
            elifNode.Condition = result.Condition;
            elifNode.Block = result.Block;
            return elifNode;
        }
        else if(Peek().Is(TokenType.LCurly))
        {
            ElseNode elseNode = new();
            elseNode.Block = ParseBlock();
            return elseNode;
        }
        else
        {
            Error(ParseError.UnexpectedToken(Peek(), TokenType.If, TokenType.LCurly), Peek());
            var recovery = ConsumeUntil(TokenType.If, TokenType.LCurly);
            if(recovery.Is(TokenType.If, TokenType.LCurly)) goto Begin;
            return new ElseNode();
        }
    }

    private MutationNode ParseMut()
    {
        MutationNode mut = new();
        Consume();

Name:

        if(Peek().Is(TokenType.Id))
        {
            mut.Name = Consume().Value;
            goto Operator;
        }
        else
        {
            Error(ParseError.UnexpectedToken(Peek(), TokenType.Id), Peek());
            var recovery = ConsumeUntil(TokenType.Id, TokenType.Semi, TokenType.RCurly);
            if(recovery.Is(TokenType.Id)) goto Name;
            return mut;
        }

Operator:

        if(Peek().IsMutOperator())
        {
            mut.Operator = Consume();
            goto Expression;
        }
        else
        {
            Error(ParseError.ExpectedMutableOperator, Peek());
            while(Peek().IsNot(TokenType.EOF, TokenType.RCurly, TokenType.Semi) &&
                  !Peek().IsMutOperator())
                Consume();
            if(Peek().IsMutOperator()) goto Operator;
            return mut;
        }

Expression:

        if(Peek().CanStartExpression())
        {
            mut.Expression = ParseExpression();
        }
        else
        {
            Error(ParseError.ExpectedExpression(Peek()), Peek());
            while(Peek().IsNot(TokenType.EOF, TokenType.RCurly, TokenType.Semi) &&
                  !Peek().CanStartExpression())
                Consume();
            if(Peek().CanStartExpression()) goto Expression;
            return mut;
        }

        return mut;
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
        else if(Peek().Is(TokenType.Minus, TokenType.Not))
        {
            var op = Consume();
            if(Peek().CanStartExpression()) { leaf = new UnaryOperatorExpressionNode(op, ParseLeaf()); }
            else
            {
                Error(ParseError.ExpectedExpression(Peek()), Peek());
                while(!Peek().CanStartExpression() && Peek().IsNot(TokenType.RCurly, TokenType.Semi, TokenType.EOF))
                    Consume();
                if(Peek().CanStartExpression()) { leaf = new UnaryOperatorExpressionNode(op, ParseLeaf()); }
                return new LiteralExpressionNode(new BoolLiteralNode(false));
            }
        }
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
