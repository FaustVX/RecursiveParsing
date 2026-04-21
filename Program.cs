using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk

var parser = new Parser();
var treeNode = parser.Parse("2^3!");
if (treeNode is null)
    return;
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
Console.Write(" = ");
Console.WriteLine(treeNode.Evaluate(new([new("x", 5)])));
