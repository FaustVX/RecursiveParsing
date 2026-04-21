using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk

var tokenizer = new Tokenizer("-3 + 2 * 4");
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextToken!.Value.Value);
} while (tokenizer.NextToken is not (null or Token.EOL));

var parser = new Parser();
var treeNode = parser.Parse("-3 + 2 * 4");
if (treeNode is null)
    return;
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
Console.Write(" = ");
Console.WriteLine(treeNode.Evaluate(new([new("x", 5)])));
