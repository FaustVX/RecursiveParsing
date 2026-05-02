using RecursiveParsing;

// https://www.youtube.com/watch?v=SToUyjAsaFk
// http://slebok.github.io/zoo/

var input = (args is [var p,..] && File.Exists(p)) ? File.ReadAllText(p) : throw new Exception();
var ast = new Parser(input).ParseFile();
;
