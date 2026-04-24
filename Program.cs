using System.Diagnostics;
using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = "{a == b ? c : d;}";

var tokenizer = new Tokenizer(input);
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextTokenSpan.ToString());
} while (tokenizer.NextToken is not (null or Token.EOL));

var parser = new Parser();
var treeNode = parser.ParseStatement(input);
if (treeNode is null)
    return;
treeNode.PrintTree(input.AsSpan(), 0);
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
if (false || ((object)treeNode) is not ExpressionNode expressionNode) // double parse
{
    Console.Write("\n");
    ((object)treeNode switch
    {
        ExpressionNode => new Func<string, TreeNode>(parser.ParseExpression),
        StatementNode => parser.ParseStatement,
        _ => throw new UnreachableException(),
    })(sb.ToString()).Print(sb.Clear());
    Console.Write(sb);
}
else
{
    Console.Write(" = ");
    Console.WriteLine(expressionNode.Evaluate(new([
        new("true", true),
        new("false", false),
        new("abs", (Delegate)decimal.Abs),
        new("rng", (Delegate)new Random().NextDouble),
        new("round", (Delegate)((decimal d, decimal decimals) => decimal.Round(d, (int)decimals))),
        ]){ Outer = new([new("a", 3)]) }));
}
