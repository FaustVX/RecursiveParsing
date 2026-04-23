using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = "a == b ? c : d";

var tokenizer = new Tokenizer(input);
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextTokenSpan.ToString());
} while (tokenizer.NextToken is not (null or Token.EOL));

var parser = new Parser();
var treeNode = parser.Parse(input);
if (treeNode is null)
    return;
treeNode.PrintTree(input.AsSpan(), 0);
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
#pragma warning disable CS0162 // Unreachable code detected
if (true) // double parse
{
    Console.Write("\n");
    parser.Parse(sb.ToString()).Print(sb.Clear());
    Console.Write(sb);
}
else
{
    Console.Write(" = ");
    Console.WriteLine(treeNode.Evaluate(new([
        new("true", true),
        new("false", false),
        new("abs", (Delegate)decimal.Abs),
        new("rng", (Delegate)new Random().NextDouble),
        new("round", (Delegate)((decimal d, decimal decimals) => decimal.Round(d, (int)decimals))),
        ])));
}
