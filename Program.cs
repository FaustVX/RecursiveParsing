using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && File.Exists(p)) ? File.ReadAllText(p) : throw new Exception();

var tokenizer = new Tokenizer(input);
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextTokenSpan.ToString());
} while (tokenizer.NextToken is not (null or Token.EOF));

var parser = new Parser().ParseExpression;
var treeNode = parser(input);
if (treeNode is null)
    return;
treeNode.PrintTree(input.AsSpan(), 0);
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
if (true) // double parse
{
#pragma warning disable CS0162 // Unreachable code detected
    Console.WriteLine();
    parser(sb.ToString()).Print(sb.Clear());
    Console.Write(sb);
#pragma warning restore CS0162 // Unreachable code detected
}
if (true && ((object)treeNode) is ExpressionNode expressionNode)
{
    Console.Write(" = ");
    Console.WriteLine(expressionNode.Evaluate(new([
        new("true", true),
        new("false", false),
        new("abs", (Delegate)decimal.Abs),
        new("rng", (Delegate)new Random().NextDouble),
        new("round", (Delegate)((decimal d, decimal decimals) => decimal.Round(d, (int)decimals))),
        new("len", (Delegate)((string s) => s.Length)),
        ]){ Outer = new([new("a", 3)]) }));
}
