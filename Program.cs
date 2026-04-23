using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

const string Input = "+(5-3)";

var tokenizer = new Tokenizer(Input);
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextTokenSpan.ToString());
} while (tokenizer.NextToken is not (null or Token.EOL));

var parser = new Parser();
var treeNode = parser.Parse(Input);
if (treeNode is null)
    return;
treeNode.PrintTree(Input.AsSpan(), 0);
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
Console.Write(" = ");
Console.WriteLine(treeNode.Evaluate(new([
    new("true", true),
    new("false", false),
    new("abs", (Delegate)decimal.Abs),
    new("rng", (Delegate)new Random().NextDouble),
    new("round", (Delegate)((decimal d) => decimal.Round(d))),
    ])));
