using RecursiveParsing.Parse;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && System.IO.File.Exists(p)) ? System.IO.File.ReadAllText(p) : throw new Exception();
var ast = new Parser(input).ParseFile();
;
