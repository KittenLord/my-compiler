using System;
using System.IO;

namespace MyCompiler;

public class Program
{
    public static void Main(string[] args)
    {
        var path = "Testing/a.txt";
        var code = File.ReadAllText(path);
        code = Prelude.Content + "\n" + code;

        RunTokenizer(code);

        Console.WriteLine();

        var tokenizer = new Tokenizer(code);
        var parser = new Parser(tokenizer);
        var tree = parser.Parse();
        var analyzer = new Analyzer(tree);
        analyzer.Analyze();




        Console.WriteLine(tree);
        foreach(var type in analyzer.Types) { Console.WriteLine(type); }
        foreach(var global in tree.Variables) Console.WriteLine(global.GetType());




        Console.ForegroundColor = ConsoleColor.Red;
        foreach(var error in parser.Errors)
        {
            Console.WriteLine(error);
            Console.WriteLine();
        }

        foreach(var error in analyzer.Errors)
        {
            Console.WriteLine(error);
            Console.WriteLine();
        }
    }

    public static void RunTokenizer(string code)
    {
        var tokenizer = new Tokenizer(code);
        Token token;
        do
        {
            token = tokenizer.Consume();
            Console.WriteLine(token.ToString() + $" {token.Position}");
        }
        while(token.Type != TokenType.EOF);
    }
}
