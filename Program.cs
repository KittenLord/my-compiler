using System;
using System.IO;

namespace MyCompiler;

public class Program
{
    public static void Main(string[] args)
    {
        var path = "Testing/a.txt";
        var code = File.ReadAllText(path);

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
