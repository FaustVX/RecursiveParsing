using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && System.IO.File.Exists(p)) ? System.IO.File.ReadAllText(p) : throw new Exception();

var tokenizer = new Tokenizer(input);
do
{
    tokenizer.ScanToken();
    Console.WriteLine(tokenizer.NextTokenSpan.ToString());
} while (tokenizer.NextToken is not (null or Token.EOF));

var parser = new Parser().ParseFile;
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
