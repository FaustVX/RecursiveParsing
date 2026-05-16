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
    public required string EBNF_File
    {
        get;
        init => field = new FileInfo(value) is { Exists: true, FullName: var path }
            ? path
            : value;
    }
    public string OutputFolder { get; init; } = "out";
    public bool WaitForDebugger { get; init; } = false;
    public bool DryRun { get; init; } = false;
    public override bool Execute()
    {
#if DEBUG
        if (WaitForDebugger && !Debugger.IsAttached)
        {
            Console.Write("Waiting for debugger ");
            do
            {
                Console.Write('.');
                Thread.Sleep(100);
            }
            while (!Debugger.IsAttached);
        }

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
            try
            {
                var treeNode = new Parser(input).ParseFile();
                CheckIdentifierVisitor id_check = new(throwOnError: false);
                treeNode!.Accept(id_check);
                if (id_check.Exceptions is { Count: not 0 } exs)
                {
                    var comparer = new IndexComparer(input.Length);
                    var isError = false;
                    foreach (var ex in exs.OrderBy(e => e.Range.Start, comparer).ThenBy(e => e.Range.End))
                    {
                        isError |= ex.Level is EBNFException.ExceptionLevel.Error;
                        Log.LogErrorFromEbnfException(ex, input, EBNF_File);
                    }
                    return !isError;
                }
                CSharpVisitor generator = new(Namespace);
                treeNode!.Accept(generator);
                if (!DryRun)
                {
                    var rel = Path.GetRelativePath(Environment.CurrentDirectory, OutputFolder);
                    Directory.CreateDirectory($"{OutputFolder}/Parse");
                    Directory.CreateDirectory($"{OutputFolder}/Tokenize");
                    Directory.CreateDirectory($"{OutputFolder}/Visitors");
                    File.WriteAllText($"{OutputFolder}/Parse/Parser.g.cs", generator.Parser.ToString());
                    File.WriteAllText($"{OutputFolder}/Parse/IVisitor.g.cs", generator.IVisitor.ToString());
                    File.WriteAllText($"{OutputFolder}/Parse/Visitor.g.cs", generator.Visitor.ToString());
                    File.WriteAllText($"{OutputFolder}/Parse/TreeNode.g.cs", generator.TreeNode.ToString());
                    File.WriteAllText($"{OutputFolder}/Tokenize/Token.g.cs", generator.Token.ToString());
                    File.WriteAllText($"{OutputFolder}/Tokenize/Tokenizer.g.cs", generator.Tokenizer.ToString());
                    File.WriteAllText($"{OutputFolder}/Visitors/TreePrintVisitor.g.cs", generator.TreePrintVisitor.ToString());
                    File.WriteAllText($"{OutputFolder}/Ext.g.cs", generator.Ext.ToString());
                }
                return true;
            }
            catch (EBNFException ex)
            {
                Log.LogErrorFromEbnfException(ex, input, EBNF_File);
                return ex.Level is not EBNFException.ExceptionLevel.Error;
            }
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, showStackTrace: true);
            return false;
        }
    }

    private sealed class IndexComparer(int length) : IComparer<Index>
    {
        public int Compare(Index x, Index y)
        => x.GetOffset(length).CompareTo(y.GetOffset(length));
    }
}
