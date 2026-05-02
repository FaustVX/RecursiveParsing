using System.Runtime.CompilerServices;
using System.Text;
using RecursiveParsing;
using EBNF = EBNFParser;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && File.Exists(p)) ? File.ReadAllText(p) : throw new Exception();

// var ebnf = new EBNF.Phases.Parse.Parser(input).ParseFile();

// var visitor0 = new EBNF.Visitors.CheckIdentifierVisitor(throwOnError: true);
// ebnf.Accept(visitor0);
// var visitor1 = new EBNF.Visitors.CSharpVisitor(nameof(RecursiveParsing), "Parser");
// ebnf.Accept(visitor1);
// PrintSB(visitor1.Parser);
// PrintSB(visitor1.IVisitor);
// PrintSB(visitor1.TreeNode);
// PrintSB(visitor1.Token);
// PrintSB(visitor1.Tokenizer);
var ast = new Parser(input).ParseFile();
;

static void PrintSB(StringBuilder sb, [CallerArgumentExpression(nameof(sb))]string expr = default!)
{
    Console.WriteLine($"- {expr}:");
    Console.WriteLine(sb);
}
