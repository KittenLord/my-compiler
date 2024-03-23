using System;

namespace MyCompiler;

public class Tokenizer
{
    public Tokenizer(string buffer) { Buffer = buffer; }

    private int Char = 0;
    private int Line = 0;

    private int Position = 0;
    private string Buffer = "";

    private Token? PeekValue;

    private Token CreateToken(TokenType type, string value = " ", int overrideLength = -1)
    {
        var token = new Token(type, Line + 1, Char + 1, value);
        PeekValue = token;

        var length = overrideLength >= 0 ? overrideLength : value.Length;

        Char += length;
        Position += length;

        return token;
    }

    public Token Consume()
    {
        Peek();

        var v = PeekValue!.Value;
        PeekValue = null;
        return v;
    }

    public Token Peek()
    {
        if (PeekValue is not null) return PeekValue.Value;

        if (Buffer == "") return CreateToken(TokenType.EOF);
        if (Position >= Buffer.Length - 1) return CreateToken(TokenType.EOF);

        while (Position < Buffer.Length && char.IsWhiteSpace(Buffer[Position]))
        {
            if(!char.IsControl(Buffer[Position])) Char++;
            Position++;

            if (Buffer[Position - 1] == '\n')
            {
                Char = 0;
                Line++;
            }
        }
        if (Position >= Buffer.Length - 1) return CreateToken(TokenType.EOF);


        if(Position + 1 < Buffer.Length && Buffer[Position] == '/' && Buffer[Position + 1] == '/')
        {
            while(Position < Buffer.Length && !char.IsWhiteSpace(Buffer[Position]))
            {
                Position++;
            }
            return this.Peek();
        }


        if (char.IsLetter(Buffer[Position])) return ReadWordToken();
        if (char.IsDigit(Buffer[Position])) return ReadNumberToken();
        if ("+-*/%=!<>".Contains(Buffer[Position])) return ReadOperatorToken();

        if (Buffer[Position] == ';') return CreateToken(TokenType.Semi);
        if (Buffer[Position] == ',') return CreateToken(TokenType.Comma);
        if (Buffer[Position] == '.') return CreateToken(TokenType.Dot);

        // {}
        if (Buffer[Position] == '{') return CreateToken(TokenType.LCurly);
        if (Buffer[Position] == '}') return CreateToken(TokenType.RCurly);

        // ()
        if (Buffer[Position] == '(') return CreateToken(TokenType.LParen);
        if (Buffer[Position] == ')') return CreateToken(TokenType.RParen);

        // []
        if (Buffer[Position] == '[') return CreateToken(TokenType.LBrack);
        if (Buffer[Position] == ']') return CreateToken(TokenType.RBrack);

        if (Buffer[Position] == '"') return ReadStringToken();


        return CreateToken(TokenType.Invalid, Buffer[Position].ToString());
    }

    private Token ReadWordToken()
    {
        string buffer = "";
        for (var pos = Position; pos < Buffer.Length && char.IsLetterOrDigit(Buffer[pos]); pos++)
        {
            buffer += Buffer[pos];
        }

        if (buffer == "let") return CreateToken(TokenType.Let, buffer);
        if (buffer == "if") return CreateToken(TokenType.If, buffer);
        if (buffer == "fn") return CreateToken(TokenType.Fn, buffer);
        if (buffer == "for") return CreateToken(TokenType.For, buffer);
        if (buffer == "while") return CreateToken(TokenType.While, buffer);
        if (buffer == "true") return CreateToken(TokenType.True, buffer);
        if (buffer == "false") return CreateToken(TokenType.False, buffer);
        if (buffer == "return") return CreateToken(TokenType.Return, buffer);
        return CreateToken(TokenType.Id, buffer);
    }

    private Token ReadNumberToken()
    {
        string buffer = "";
        int pos;
        for (pos = Position;
            pos < Buffer.Length &&
                (char.IsDigit(Buffer[pos]) || "xbo._df".Contains(Buffer[pos]));
            pos++)
        {
            buffer += Buffer[pos];
        }

        return CreateToken(TokenType.Number, buffer);
    }

    private Token ReadOperatorToken()
    {
        int pos = Position;
        string buffer = "";

        if (pos < Buffer.Length) buffer += Buffer[pos++];
        if (pos < Buffer.Length) buffer += Buffer[pos++];
        if (pos < Buffer.Length) buffer += Buffer[pos++];

        if (buffer == "%%=") return CreateToken(TokenType.ModNegAsgn, buffer);

        buffer = buffer.Substring(0, 2);

        if (buffer == "+=") return CreateToken(TokenType.PlusAsgn, buffer);
        if (buffer == "-=") return CreateToken(TokenType.MinusAsgn, buffer);
        if (buffer == "*=") return CreateToken(TokenType.MulAsgn, buffer);
        if (buffer == "/=") return CreateToken(TokenType.DivAsgn, buffer);
        if (buffer == "%=") return CreateToken(TokenType.ModAsgn, buffer);

        if (buffer == "%%") return CreateToken(TokenType.ModNeg, buffer);

        if (buffer == "==") return CreateToken(TokenType.Eq, buffer);
        if (buffer == "!=") return CreateToken(TokenType.Neq, buffer);
        if (buffer == "<=") return CreateToken(TokenType.LsEq, buffer);
        if (buffer == ">=") return CreateToken(TokenType.GrEq, buffer);

        if (buffer == "->") return CreateToken(TokenType.RetArrow, buffer);

        buffer = buffer.Substring(0, 1);

        if (buffer == "+") return CreateToken(TokenType.Plus, buffer);
        if (buffer == "-") return CreateToken(TokenType.Minus, buffer);
        if (buffer == "*") return CreateToken(TokenType.Mul, buffer);
        if (buffer == "/") return CreateToken(TokenType.Div, buffer);
        if (buffer == "%") return CreateToken(TokenType.Mod, buffer);

        if (buffer == "=") return CreateToken(TokenType.Assign, buffer);
        if (buffer == "!") return CreateToken(TokenType.Not, buffer);
        if (buffer == "<") return CreateToken(TokenType.Ls, buffer);
        if (buffer == ">") return CreateToken(TokenType.Gr, buffer);

        throw new Exception("This isn't intended");
    }

    private Token ReadStringToken()
    {
        string buffer = ""; 
        bool escaped = false;
        int pos;
        for(pos = Position + 1; pos < Buffer.Length; pos++)
        {
            if(escaped)
            {
                if(Buffer[pos] == '\\') { buffer += '\\'; }
                if(Buffer[pos] == '"') { buffer += '"'; }

                // TODO: google about other escape sequences
                if(Buffer[pos] == 'n') { buffer += '\n'; }
                if(Buffer[pos] == 'r') { buffer += '\r'; }
                if(Buffer[pos] == 't') { buffer += '\t'; }

                escaped = false;
                continue;
            }

            if(Buffer[pos] == '\\') { escaped = true; continue; }
            if(Buffer[pos] == '"') return CreateToken(TokenType.String, buffer, pos + 1 - Position);
            buffer += Buffer[pos];
        }

        return CreateToken(TokenType.Invalid, buffer, pos - Position);
    }
}
