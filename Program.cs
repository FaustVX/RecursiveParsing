using System.Diagnostics;
using System.Text;
using RecursiveParsing.Phases.Parse;
using RecursiveParsing.Phases.Tokenize;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && System.IO.File.Exists(p)) ? System.IO.File.ReadAllText(p) : throw new Exception();
var printSteps = Print.All;
var doubleParseSteps = Print.All;

if (printSteps.HasFlag(Print.Tokens)) // print tokens
{
    var tokenizer = new Tokenizer(input);
    do
    {
        tokenizer.ScanToken();
        Console.WriteLine(tokenizer.NextTokenSpan.ToString());
    } while (tokenizer.NextToken is not (null or Token.EOF));
    Console.WriteLine();
}

TreeNode treeNode = new Parser(input).ParseFile();
if (printSteps.HasFlag(Print.Tree)) // print tree
{
    treeNode.PrintTree(input.AsSpan(), 0);
    Console.WriteLine();
}

var sb = new StringBuilder();
treeNode.Print(sb);
if (printSteps.HasFlag(Print.Pretty)) // pretty-print
    Console.WriteLine(sb);
if (doubleParseSteps != Print.None) // double parse
{
    input = sb.ToString();

    if (doubleParseSteps.HasFlag(Print.Tokens)) // print tokens
    {
        var tokenizer = new Tokenizer(input);
        do
        {
            tokenizer.ScanToken();
            Console.WriteLine(tokenizer.NextTokenSpan.ToString());
        } while (tokenizer.NextToken is not (null or Token.EOF));
        Console.WriteLine();
    }
    treeNode = treeNode switch
    {
        RecursiveParsing.Phases.Parse.File => new Parser(input).ParseFile(),
        Expression => new Parser(input).ParseExpression(),
        _ => throw new UnreachableException(),
    };
    if (doubleParseSteps.HasFlag(Print.Tree)) // print tree
    {
        treeNode.PrintTree(input.AsSpan(), 0);
        Console.WriteLine();
    }
    treeNode.Print(sb.Clear());
    if (doubleParseSteps.HasFlag(Print.Pretty)) // pretty-print
        Console.WriteLine(sb);
}

[Flags]
enum Print
{
    None = 0,
    Tokens = 0b1 << 0,
    Tree = 0b1 << 1,
    Pretty = 0b1 << 2,
    All = int.MaxValue,
}
