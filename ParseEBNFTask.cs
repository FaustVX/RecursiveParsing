using EBNFParser.Phases.Tokenize;
using EBNFParser.Phases.Parse;
using EBNFParser.Visitors;

namespace EBNFParser;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;
using System.IO;

// https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation
// https://manski.net/articles/msbuild/custom-tasks

public sealed class ParseEBNFTask : Task
{
    [Required]
    public required string Namespace { get; init; }
    [Required]
    public required string EBNF_File { get; init; }
    public string OutputFolder { get; init; } = "out";
    public override bool Execute()
    {
#if DEBUG
        Debugger.Break();
#endif
        try
        {
            if (!File.Exists(EBNF_File))
            {
                Log.LogError($"'{EBNF_File}' don't exist");
                return false;
            }
            var input = File.ReadAllText(EBNF_File);
            TreeNode? treeNode = new Parser(input).ParseFile();
            CheckIdentifierVisitor id_check = new(throwOnError: true);
            treeNode!.Accept(id_check);
            CSharpVisitor generator = new(Namespace);
            treeNode!.Accept(generator);
            var rel = Path.GetRelativePath(Environment.CurrentDirectory, OutputFolder);
            if (!string.IsNullOrWhiteSpace(OutputFolder))
            {
                // if (Directory.Exists($"{OutputFolder}"))
                //     Directory.Delete($"{OutputFolder}", recursive: true);
                Directory.CreateDirectory($"{OutputFolder}");
            }
            Directory.CreateDirectory($"{OutputFolder}/Parse");
            Directory.CreateDirectory($"{OutputFolder}/Tokenize");
            Directory.CreateDirectory($"{OutputFolder}/Visitors");
            File.WriteAllText($"{OutputFolder}/Parse/Parser.g.cs", generator.Parser.ToString());
            File.WriteAllText($"{OutputFolder}/Parse.IVisitor.g.cs", generator.IVisitor.ToString());
            File.WriteAllText($"{OutputFolder}/Parse/TreeNode.g.cs", generator.TreeNode.ToString());
            File.WriteAllText($"{OutputFolder}/Tokenize/Token.g.cs", generator.Token.ToString());
            File.WriteAllText($"{OutputFolder}/Tokenize/Tokenizer.g.cs", generator.Tokenizer.ToString());
            File.WriteAllText($"{OutputFolder}/Visitors/TreePrintVisitor.g.cs", generator.TreePrintVisitor.ToString());
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }
}
