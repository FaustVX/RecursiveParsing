using System.Text;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class CSharpVisitor : IVisitor
{
    public StringBuilder Parser { get; } = new();
    public StringBuilder IVisitor { get; } = new();
    public StringBuilder TreeNode { get; } = new();
    private readonly PrettyPrintVisitor prettyPrintVisitor = new();
    void IVisitor.Enter(Phases.Parse.File file)
    {
        IVisitor.AppendLine($$"""
        namespace EBNFParser.Phases.Parse;

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
        TreeNode.AppendLine($$"""
        namespace EBNFParser.Phases.Parse;

        public abstract record class TreeNode(Range Span)
        {
            public abstract void Accept(IVisitor visitor);
        }
        """);
    }
    void IVisitor.Enter(Declaration declaration)
    {
        declaration.Accept(prettyPrintVisitor);
        var ebnf = OutputAndClear(prettyPrintVisitor.StringBuilder);
        Parser.Append($$"""
            /// <summary>
            /// {{ebnf}}
            /// </summary>
            private {{declaration.Id.Name}} Parse_{{declaration.Id.Name}}(Tokenizer tokenizer)
            {
                var start = tokenizer.CurrentSpan.Start;
        
        """);
        TreeNode.AppendLine($$"""

        /// <summary>
        /// {{ebnf}}
        /// </summary>
        public record class {{declaration.Id.Name}}(Range Span) : TreeNode(Span)
        {
            public override void Accept(IVisitor visitor)
            {
                visitor.Enter(this);
                visitor.Visit(this);
                visitor.Exit(this);
            }
        }
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
