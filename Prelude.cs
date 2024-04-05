namespace MyCompiler;

public static class Prelude
{
    public const string Content = 
@"
fn new(int num) -> int { 0 }
let int NULL = 0;

fn prelude() {
    main();
}
";

    public static int Lines => Content.Split("\n").Length;

}
