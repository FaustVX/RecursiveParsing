using System.Text;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class CSharpVisitor : IVisitor
{
    public StringBuilder Parser { get; } = new();
    public StringBuilder IVisitor { get; } = new();
    public StringBuilder TreeNode { get; } = new();
    private readonly PrettyPrintVisitor prettyPrintVisitor = new();
    private bool _isDeclarationBody = false;
    private int _depth = 0;
    private static readonly Dictionary<int, string> _indent = [];
    private string IndentSpaces(int depth)
    {
        ref var indent = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 4]);
        s.Fill(' ');
        return indent = new string(s);
    }
    private void PrintTree(TreeNode node, bool isTerminal)
    {
        node.Accept(prettyPrintVisitor);
        Parser.AppendLine($"{IndentSpaces(2)}// {IndentSpaces(_depth)}Parse_{node.GetType().Name} ({OutputAndClear(prettyPrintVisitor.StringBuilder, escapeXML: false)}){(isTerminal ? ";" : ":")}");
    }

    void IVisitor.Enter(Phases.Parse.File file)
    {
        IVisitor.AppendLine($$"""
        namespace EBNFParser.Phases.Parse;

        public partial interface IVisitor
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

        public partial abstract record class TreeNode(Range Span)
        {
            public abstract void Accept(IVisitor visitor);
        }
        """);
    }
    void IVisitor.Enter(Declaration declaration)
    {
        declaration.Accept(prettyPrintVisitor);
        var ebnf = OutputAndClear(prettyPrintVisitor.StringBuilder, escapeXML: true);
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
        public partial record class {{declaration.Id.Name}}(Range Span) : TreeNode(Span)
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
    void IVisitor.Visit(Declaration declaration)
    {
        _isDeclarationBody = true;
    }
    void IVisitor.Enter(Choice choice)
    {
        PrintTree(choice, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Choice choice)
    {
        _depth--;
    }
    void IVisitor.Enter(Sequence sequence)
    {
        PrintTree(sequence, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Sequence sequence)
    {
        _depth--;
    }
    void IVisitor.Enter(Postfix postfix)
    {
        PrintTree(postfix, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Postfix postfix)
    {
        _depth--;
    }
    void IVisitor.Visit(Primary primary)
    {
        if (_isDeclarationBody)
            PrintTree(primary, isTerminal: true);
    }
    void IVisitor.Exit(Declaration declaration)
    {
        _isDeclarationBody = false;
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

    private static string OutputAndClear(StringBuilder sb, bool escapeXML)
    {
        var output = (escapeXML ? sb.Replace("<", "&lt;").Replace(">", "&gt;") : sb).ToString();
        sb.Clear();
        return output;
    }
}
