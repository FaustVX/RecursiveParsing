using System.Text;
using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk

var parser = new Parser();
var treeNode = parser.Parse("3! != 2!");
if (treeNode is null)
    return;
var sb = new StringBuilder();
treeNode.Print(sb);
Console.Write(sb);
Console.Write(" = ");
Console.WriteLine(treeNode.Evaluate(new([new("true", 1), new("false", 0)])));
