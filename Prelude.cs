namespace MyCompiler;

public static class Prelude
{
    public const string Contents = 
@"
fn new(int num) -> int { 0 }
let int NULL = 0;

fn prelude() {
    main();
}
";

}
