﻿using System;
using System.IO;

namespace MyCompiler;

public class Program
{
    public static void Main(string[] args)
    {
        var path = "Testing/a.txt";
        var code = File.ReadAllText(path);

        RunTokenizer(code);
        var tokenizer = new Tokenizer(code);
        var parser = new Parser(tokenizer);
        var result = parser.Parse();

        System.Console.WriteLine($"{result}");
        System.Console.WriteLine($"\n{parser.Success}");
    }

    public static void RunTokenizer(string code)
    {
        var tokenizer = new Tokenizer(code);
        Token token;
        do
        {
            token = tokenizer.Consume();
            Console.WriteLine(token.ToString());
        }
        while(token.Type != TokenType.EOF);
    }
}
