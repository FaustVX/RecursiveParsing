using System.Diagnostics;
using System.Text;
using RecursiveParsing.Phases.Parse;
using RecursiveParsing.Phases.Tokenize;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && System.IO.File.Exists(p)) ? System.IO.File.ReadAllText(p) : throw new Exception();
var printSteps = Print.All;
var doubleParseSteps = Print.All;

TreeNode? treeNode = null;
var sb = Parse(input, ref treeNode, printSteps);

if (doubleParseSteps != Print.None)
{
    Console.WriteLine("Double parsing...");
    input = sb.ToString();
    sb = Parse(input, ref treeNode, doubleParseSteps);
    Debug.Assert(input == sb.ToString());
}

static StringBuilder Parse(string input, ref TreeNode? treeNode, Print mode)
{
    if (mode.HasFlag(Print.Tokens))
        PrintTokens(input);
    treeNode = treeNode switch
    {
        RecursiveParsing.Phases.Parse.File => new Parser(input).ParseFile(),
        Expression => new Parser(input).ParseExpression(),
        null => new Parser(input).ParseFile(),
        _ => throw new UnreachableException(),
    };
    if (mode.HasFlag(Print.Tree))
        PrintTree(input, treeNode);

    var sb = new StringBuilder();
    treeNode.Print(sb);
    if (mode.HasFlag(Print.Pretty))
        PrettyPrint(sb);
    return sb;
}

static void PrintTokens(string input)
{
    var tokenizer = new Tokenizer(input);
    do
    {
        tokenizer.ScanToken();
        Console.WriteLine(tokenizer.CurrentTokenSpan.ToString());
    } while (tokenizer.CurrentToken is not (null or Token.EOF));
    Console.WriteLine();
}

static void PrintTree(string input, TreeNode treeNode)
{
    treeNode.PrintTree(input.AsSpan(), 0);
    Console.WriteLine();
}

static void PrettyPrint(StringBuilder sb)
{
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
