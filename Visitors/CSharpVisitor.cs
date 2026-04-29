using System.Text;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class CSharpVisitor : IVisitor
{
    public StringBuilder Parser { get; } = new();
    public StringBuilder IVisitor { get; } = new();
    private readonly PrettyPrintVisitor prettyPrintVisitor = new();
    void IVisitor.Enter(Phases.Parse.File file)
    {
        IVisitor.AppendLine($$"""
        public interface IVisitor
        {
        """);
        Parser.AppendLine("""
        using System.Collections.Immutable;
        using EBNFParser.Phases.Tokenize;

        namespace EBNFParser.Phases.Parse;
        public partial class Parser
        {
        """);
    }
    void IVisitor.Enter(Declaration declaration)
    {
        declaration.Accept(prettyPrintVisitor);
        Parser.Append($$"""
            /// <summary>
            /// {{OutputAndClear(prettyPrintVisitor.StringBuilder)}}
            /// </summary>
            private {{declaration.Id.Name}} Parse_{{declaration.Id.Name}}(Tokenizer tokenizer)
            {
                var start = tokenizer.CurrentSpan.Start;
        
        """);
    }
    void IVisitor.Exit(Declaration declaration)
    {
        Parser.AppendLine($$"""
                var end = tokenizer.CurrentSpan.End;
                return new {{declaration.Id.Name}}(statements, start..end);
            }
        """);
        IVisitor.AppendLine($$"""    void Enter({{declaration.Id.Name}} {{declaration.Id.Name}}) {}""");
        IVisitor.AppendLine($$"""    void Visit({{declaration.Id.Name}} {{declaration.Id.Name}}) {}""");
        IVisitor.AppendLine($$"""    void Exit({{declaration.Id.Name}} {{declaration.Id.Name}}) {}""");
    }
    void IVisitor.Exit(Phases.Parse.File file)
    {
        IVisitor.AppendLine("}");
        Parser.AppendLine("}");
    }

    public static string OutputAndClear(StringBuilder sb)
    {
        var output = sb.Replace("<", "&lt;").Replace(">", "&gt;").ToString();
        sb.Clear();
        return output;
    }
}
