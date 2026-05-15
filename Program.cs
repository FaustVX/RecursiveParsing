using System.Text;
using RecursiveParsing.Parse;
using RecursiveParsing.Visitors;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && new FileInfo(p) is { Exists: true, Extension: ".txt" }) ? System.IO.File.ReadAllText(p) : throw new Exception();
var ast = new Parser(input).ParseFile();
// ast.Accept(new TreePrintVisitor(input.AsMemory()));
var qbe = new QBEVisitor();
ast.Accept(qbe);
System.IO.File.WriteAllBytes(@"obj/main.ssa", Encoding.ASCII.GetBytes(qbe.QBEFile.ToString()));
;
