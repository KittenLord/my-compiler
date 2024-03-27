using System;
using System.IO;

namespace MyCompiler;

public class Program
{
    public static void Main(string[] args)
    {
        var path = "Testing/a.txt";
        var code = File.ReadAllText(path);

        RunTokenizer(code);

        Console.WriteLine();

        var tokenizer = new Tokenizer(code);
        var parser = new Parser(tokenizer);
        var result = parser.Parse();

        Console.ForegroundColor = ConsoleColor.Red;
        foreach(var error in parser.Errors)
        {
            Console.WriteLine(error);
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(result);
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
