using System.Text;
using RecursiveParsing;
using RecursiveParsing.Parse;
using RecursiveParsing.Tokenize;
using RecursiveParsing.Visitors;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && new FileInfo(p) is { Exists: true, Extension: ".txt", FullName: var f }) ? System.IO.File.ReadAllText(f) : throw new Exception();
var name = Path.GetFileNameWithoutExtension(f);
try
{
    var ast = new Parser(input).ParseFile();
    // ast.Accept(new TreePrintVisitor(input.AsMemory()));
    ast.Accept(new CheckTypeVisitor(new()
    {
        [(new Token.Id("printf"), [ExpressionType.Int])] = ExpressionType.None,
        [(new Token.Id("printf"), [ExpressionType.String])] = ExpressionType.None,
        [(new Token.Symbol("+"), [ExpressionType.Int])] = ExpressionType.Int,
        [(new Token.Symbol("-"), [ExpressionType.Int])] = ExpressionType.Int,
        // [(new Token.Symbol("!"), [ExpressionType.Int])] = ExpressionType.Int,
        [(new Token.Symbol("+"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Int,
        [(new Token.Symbol("-"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Int,
        [(new Token.Symbol("*"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Int,
        [(new Token.Symbol("/"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Int,
        // [(new Token.Symbol("^"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Int,
        // [(new Token.Symbol("=="), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Bool,
        // [(new Token.Symbol("!="), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Bool,
        // [(new Token.Symbol("<="), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Bool,
        // [(new Token.Symbol("<"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Bool,
        // [(new Token.Symbol(">="), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Bool,
        // [(new Token.Symbol(">"), [ExpressionType.Int, ExpressionType.Int])] = ExpressionType.Bool,
        // [(new Token.Symbol("=="), [ExpressionType.String, ExpressionType.String])] = ExpressionType.Bool,
        // [(new Token.Symbol("!="), [ExpressionType.String, ExpressionType.String])] = ExpressionType.Bool,
        // [(new Token.Symbol("=="), [ExpressionType.Bool, ExpressionType.Bool])] = ExpressionType.Bool,
        // [(new Token.Symbol("<="), [ExpressionType.Bool, ExpressionType.Bool])] = ExpressionType.Bool,
        // [(new Token.Symbol("+"), [ExpressionType.String, ExpressionType.String])] = ExpressionType.String,
    }));
    var qbe = new QBEVisitor();
    ast.Accept(qbe);
    System.IO.File.WriteAllBytes(@$"obj/{name}.ssa", Encoding.ASCII.GetBytes(qbe.QBEFile.ToString().Replace("\r\n", "\n")));
}
catch (EBNFException ex)
{
    var (l, c) = ex.Range.Start.GetPos(input);
    Console.WriteLine($"[{f}:{l}:{c}]: {ex.SubCategory} {ex.ErrorCode}: {ex.Message}");
    return 1;
}
return 0;
