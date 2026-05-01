using System.Runtime.CompilerServices;
using System.Text;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class CSharpVisitor(string @namespace, string parserClass) : IVisitor
{
    public StringBuilder Parser { get; } = new();
    public StringBuilder IVisitor { get; } = new();
    public StringBuilder TreeNode { get; } = new();
    public string Namespace { get; } = @namespace;
    public string ParserClass { get; } = parserClass;

    private readonly PrettyPrintVisitor prettyPrintVisitor = new();
    private bool _isDeclarationBody = false;
    private int _depth = 1;
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
        Parser.AppendLine($"{IndentSpaces(1)}/// {IndentSpaces(_depth - 1)}Parse_{node.GetType().Name} ({OutputAndClear(prettyPrintVisitor, escapeXML: false)}){(isTerminal ? ";" : ":")}");
    }

    void IVisitor.Enter(Phases.Parse.File file)
    {
        IVisitor.AppendLine($$"""
        namespace {{Namespace}};

        public partial interface IVisitor
        {
        """);
        Parser.AppendLine($$"""
        namespace {{Namespace}};
        public partial class {{ParserClass}}
        {
        """);
        TreeNode.AppendLine($$"""
        namespace {{Namespace}};

        public abstract partial record class TreeNode(Range Span)
        {
            public abstract void Accept(IVisitor visitor);
        }
        """);
    }
    void IVisitor.Enter(Declaration declaration)
    {
        declaration.Accept(prettyPrintVisitor);
        var ebnf = OutputAndClear(prettyPrintVisitor, escapeXML: true);
        Parser.AppendLine($$"""
            /// <summary>
            /// <c>{{ebnf}}</c>
            /// </summary>
            /// <remarks>
            /// <code>
            /// var start = tokenizer.CurrentSpan.Start;
        """);
        TreeNode.AppendLine($$"""

        /// <summary>
        /// <code>{{ebnf}}</code>
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
            /// var end = tokenizer.CurrentSpan.End;
            /// return new {{declaration.Id.Name}}(statements, start..end);
            /// </code>
            /// </remarks>
            private partial {{declaration.Id.Name}} Parse_{{declaration.Id.Name}}(Tokenizer tokenizer);

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

    private static string OutputAndClear(PrettyPrintVisitor visitor , bool escapeXML)
    {
        var output = (escapeXML ? visitor.StringBuilder.Replace("<", "&lt;").Replace(">", "&gt;") : visitor.StringBuilder).ToString();
        visitor.StringBuilder.Clear();
        Precedences(visitor).Clear();
        Precedences(visitor).Push((NodePrecedence)0);
        return output;

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_precedences")]
        static extern ref readonly Stack<NodePrecedence> Precedences(PrettyPrintVisitor visitor);
    }
}
