using System.Diagnostics;
using EBNFParser.Phases.Parse;
using EBNFParser.Phases.Tokenize;
using EBNFParser.Visitors;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p, ..] && System.IO.File.Exists(p)) ? System.IO.File.ReadAllText(p) : throw new Exception();
var printSteps = Print.CSharp;
var doubleParseSteps = Print.None;

TreeNode? treeNode = null;
var sb = Parse(input, ref treeNode, printSteps);
treeNode!.Accept(new CheckIdentifierVisitor(throwOnError: false));

if (doubleParseSteps != Print.None)
{
    Console.WriteLine("Double parsing...");
    input = sb;
    sb = Parse(input, ref treeNode, doubleParseSteps);
    Debug.Assert(input == sb);
}

static string Parse(string input, ref TreeNode? treeNode, Print mode)
{
    if (mode.HasFlag(Print.Tokens))
        PrintTokens(input);
    treeNode = treeNode switch
    {
        EBNFParser.Phases.Parse.File => new Parser(input).ParseFile(),
        Expression => new Parser(input).ParseExpression(),
        null => new Parser(input).ParseFile(),
        _ => throw new UnreachableException(),
    };
    if (mode.HasFlag(Print.Tree))
        PrintTree(input, treeNode);
    if (mode.HasFlag(Print.CSharp))
        CSharpPrint(treeNode);
    if (mode.HasFlag(Print.Pretty))
        input = PrettyPrint(treeNode);
    return input;
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
    treeNode.Accept(new TreePrintVisitor(input.AsMemory()));
    Console.WriteLine();
}

static string PrettyPrint(TreeNode node)
{
    var visitor = new PrettyPrintVisitor();
    node.Accept(visitor);
    string value = visitor.StringBuilder.ToString();
    Console.WriteLine(value);
    return value;
}

static void CSharpPrint(TreeNode treeNode)
{
    CSharpVisitor visitor = new("EBNFParser.Phases.Parse", "Parser");
    treeNode!.Accept(visitor);
    Console.WriteLine(visitor.Parser);
    Console.WriteLine(visitor.IVisitor);
    Console.WriteLine(visitor.TreeNode);
}

[Flags]
enum Print
{
    None = 0,
    Tokens = 0b1 << 0,
    Tree = 0b1 << 1,
    Pretty = 0b1 << 2,
    CSharp = 0b1 << 3,
    All = int.MaxValue,
}
