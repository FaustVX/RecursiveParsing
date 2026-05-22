using System.Text;
using RecursiveParsing;
using RecursiveParsing.Parse;
using RecursiveParsing.Tokenize;
using RecursiveParsing.Visitors;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && new FileInfo(p) is { Exists: true, Extension: ".txt", FullName: var f }) ? System.IO.File.ReadAllText(f) : throw new Exception();
var name = Path.GetFileNameWithoutExtension(f);
TreeNode? ast = default;
try
{
    ast = new Parser(input).ParseFile();
    var functions = new Dictionary<(Token, System.Collections.Immutable.ImmutableArray<ExpressionTypeUnion>), (ExpressionTypeUnion type, string funcName)>()
    {
        [(new Token.Id("println"), [ExpressionType.Int])] = (ExpressionType.None, "_println_int"),
        [(new Token.Id("print"), [ExpressionType.Int])] = (ExpressionType.None, "_print_int"),
        [(new Token.Id("println"), [ExpressionType.String])] = (ExpressionType.None, "_println_str"),
        [(new Token.Id("print"), [ExpressionType.String])] = (ExpressionType.None, "_print_str"),
        [(new Token.Id("println"), [ExpressionType.Bool])] = (ExpressionType.None, "_println_bool"),
        [(new Token.Id("print"), [ExpressionType.Bool])] = (ExpressionType.None, "_print_bool"),
        [(new Token.Id("time"), [])] = (ExpressionType.Int, "_time"),
        [(new Token.Id("rand"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Int, "_rand_min_max"),
        [(new Token.Id("rand"), [ExpressionType.Int])] = (ExpressionType.Int, "_rand_max"),
        [(new Token.Id("rand"), [])] = (ExpressionType.Int, "_rand"),
        [(new Token.Symbol("+"), [ExpressionType.Int])] = (ExpressionType.Int, ""),
        [(new Token.Symbol("-"), [ExpressionType.Int])] = (ExpressionType.Int, ""),
        [(new Token.Symbol("+"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Int, "add"),
        [(new Token.Symbol("-"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Int, "sub"),
        [(new Token.Symbol("*"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Int, "mul"),
        [(new Token.Symbol("/"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Int, "div"),
        [(new Token.Symbol("=="), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Bool, "ceqw"),
        [(new Token.Symbol("!="), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Bool, "cnew"),
        [(new Token.Symbol("<="), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Bool, "cslew"),
        [(new Token.Symbol("<"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Bool, "csltw"),
        [(new Token.Symbol(">="), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Bool, "csgew"),
        [(new Token.Symbol(">"), [ExpressionType.Int, ExpressionType.Int])] = (ExpressionType.Bool, "csgtw"),
        [(new Token.Symbol("=="), [ExpressionType.String, ExpressionType.String])] = (ExpressionType.Bool, "_streq"),
        [(new Token.Symbol("!="), [ExpressionType.String, ExpressionType.String])] = (ExpressionType.Bool, "_strne"),
        [(new Token.Symbol("=="), [ExpressionType.Bool, ExpressionType.Bool])] = (ExpressionType.Bool, "ceqw"),
        [(new Token.Symbol("<="), [ExpressionType.Bool, ExpressionType.Bool])] = (ExpressionType.Bool, "ceqw"),
        [(new Token.Symbol("+"), [ExpressionType.String, ExpressionType.String])] = (ExpressionType.String, "_strconcat"),
    };
    ast.Accept(new CheckTypeVisitor(functions));
    var qbe = new QBEVisitor(functions);
    ast.Accept(qbe);
    System.IO.File.WriteAllBytes(@$"obj/{name}.ssa", Encoding.ASCII.GetBytes(qbe.QBEFile.ToString().Replace("\r\n", "\n")));
}
catch (EBNFException ex)
{
    ast?.Accept(new TreePrintVisitor(input.AsMemory()));
    ast?.Accept(new TreeTypePrintVisitor(input.AsMemory()));
    var (l, c) = ex.Range.Start.GetPos(input);
    Console.WriteLine($"[{f}:{l}:{c}]: {ex.SubCategory} {ex.ErrorCode}: {ex.Message}");
    return 1;
}
return 0;
