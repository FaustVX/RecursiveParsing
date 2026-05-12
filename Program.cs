using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using EBNFParser.Phases.Parse;
using EBNFParser.Phases.Tokenize;
using EBNFParser.Visitors;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p, var @namespace] && new FileInfo(p) is { Exists: true, Extension: ".ebnf" }) ? System.IO.File.ReadAllText(p) : throw new Exception();
var printSteps = Print.CSharp;
var doubleParseSteps = Print.None;

TreeNode? treeNode = null;
var sb = Parse(input, ref treeNode, printSteps, @namespace);
treeNode!.Accept(new CheckIdentifierVisitor(throwOnError: false));

if (doubleParseSteps != Print.None)
{
    Console.WriteLine("Double parsing...");
    input = sb;
    var previousAST = treeNode;
    sb = Parse(input, ref treeNode, doubleParseSteps, @namespace);
    Debug.Assert(input == sb);
    Debug.Assert(previousAST.Equals(treeNode, TreeNode.EqualityComparer.Instance));
}

static string Parse(string input, ref TreeNode? treeNode, Print mode, string @namespace)
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
        CSharpPrint(treeNode, @namespace);
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

static void CSharpPrint(TreeNode treeNode, string @namespace)
{
    CSharpVisitor visitor = new(@namespace);
    treeNode!.Accept(visitor);
    PrintSB(visitor.Parser);
    PrintSB(visitor.IVisitor);
    PrintSB(visitor.TreeNode);
    PrintSB(visitor.Token);
    PrintSB(visitor.Tokenizer);
    PrintSB(visitor.TreePrintVisitor);
    static void PrintSB(StringBuilder sb, [CallerArgumentExpression(nameof(sb))]string expr = default!)
    {
        Console.WriteLine($"- {expr}:");
        Console.WriteLine(sb);
    }
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
