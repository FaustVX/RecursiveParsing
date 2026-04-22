using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk

const string Input = "abs(-5) == rng()\n";

var tokenizer = new Tokenizer(Input);
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextToken!.Value.ToString());
} while (tokenizer.NextToken is not (null or Token.EOL));

var parser = new Parser();
var treeNode = parser.Parse(Input);
if (treeNode is null)
    return;
var sb = new StringBuilder();
treeNode.PrintTree(0);
treeNode.Print(sb);
Console.Write(sb);
Console.Write(" = ");
var rng = new Random();
Console.WriteLine(treeNode.Evaluate(new([new("true", true), new("false", false), new ("abs", (Delegate)decimal.Abs), new("rng", (Delegate)(() => rng.Next()))])));
