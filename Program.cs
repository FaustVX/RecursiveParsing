using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk

const string Input = "3! != 2!";

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
Console.WriteLine(treeNode.Evaluate(new([new("true", 1), new("false", 0)])));
