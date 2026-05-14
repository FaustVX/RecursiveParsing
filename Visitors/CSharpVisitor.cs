using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class CSharpVisitor(string @namespace) : IVisitor
{
    public StringBuilder Parser { get; } = new();
    public StringBuilder IVisitor { get; } = new();
    public StringBuilder Visitor { get; } = new();
    public StringBuilder TreeNode { get; } = new();
    public StringBuilder Token { get; } = new();
    public StringBuilder Tokenizer { get; } = new();
    public StringBuilder TreePrintVisitor { get; } = new();
    public string Namespace { get; } = @namespace;
    public Dictionary<Primary, string> IdToCSharp { get; private set; } = null!;

    private readonly PrettyPrintVisitor prettyPrintVisitor = new();
    private bool? _isDeclarationBody = null;
    private bool _isInNode = false;
    private int _depth = 1;
    private static readonly Dictionary<int, string> _indent = [];
    private string IndentSpaces(int depth)
    {
        ref var indent = ref CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 4]);
        s.Fill(' ');
        return indent = new string(s);
    }
    private void PrintTreeDocumentation(TreeNode node, bool isTerminal)
    {
        if (_isInNode)
            return;
        node.Accept(prettyPrintVisitor);
        Parser.AppendLine($"{IndentSpaces(1)}/// {IndentSpaces(_depth - 1)}Parse_{node.GetType().Name} ({OutputAndClear(prettyPrintVisitor, escapeXML: true)}){(isTerminal ? ";" : ":")}");
    }

    void IVisitor.Enter(Phases.Parse.File file)
    {
        var id = new IdToCSharpVisitor();
        file.Accept(id);
        IdToCSharp = id.Names;

        IVisitor.AppendLine($$"""
        #nullable enable
        namespace {{Namespace}}.Parse;

        public partial interface IVisitor
        {
        """);

        Visitor.AppendLine($$"""
        #nullable enable
        namespace {{Namespace}}.Parse;

        public partial class Visitor : IVisitor
        {
        """);
        Parser.AppendLine($$"""
        #nullable enable
        using System.Collections.Immutable;
        using {{Namespace}}.Tokenize;

        namespace {{Namespace}}.Parse;

        public partial class Parser(string input)
        {
            public Tokenizer Tokenizer { get; } = new(input);

        """);
        TreeNode.AppendLine($$"""
        #nullable enable
        using System.Collections;
        using System.Collections.Immutable;
        using {{Namespace}}.Tokenize;

        namespace {{Namespace}}.Parse;

        public abstract partial record class TreeNode(Range Span)
        {
            public abstract void Accept(IVisitor visitor);
            protected static bool StructuralEquals<T>(T lhs, T? rhs)
            where T : struct, IStructuralEquatable
            => rhs?.Equals(lhs, EqualityComparer.Instance) ?? false;
            protected static bool StructuralEquals<T>(T lhs, T? rhs)
            where T : class, IStructuralEquatable
            => rhs?.Equals(lhs, EqualityComparer.Instance) ?? false;
            public bool Equals(object? other, IEqualityComparer comparer)
            => other is TreeNode node && comparer.Equals(this, node);
            public int GetHashCode(IEqualityComparer comparer)
            => comparer.GetHashCode(this);

            public sealed class EqualityComparer : IEqualityComparer
            {
                private EqualityComparer() {}
                public static EqualityComparer Instance { get; set; } = new();

                public new bool Equals(object? x, object? y)
                => (x, y) switch
                {
        """);
        foreach (var node in file.Nodes)
            if (node.Id.Operator.Token is not Phases.Tokenize.Token.Symbol { Value: "*" })
                TreeNode.AppendLine($$"""            ({{IdToCSharp[(Primary)node.Id.Node]}} lhs, {{IdToCSharp[(Primary)node.Id.Node]}} rhs) => lhs.Equals(rhs),""");
        TreeNode.AppendLine($$"""
                    _ => false,
                };

                public int GetHashCode(object obj)
                => obj.GetHashCode();
            }
        }
        """);
        Token.AppendLine($$"""
        #nullable enable
        using System.Diagnostics.CodeAnalysis;
        using System.Runtime.CompilerServices;
        using System.Text;

        namespace {{Namespace}}.Tokenize;

        public readonly record struct TokenSpan(Token.WhiteSpace Before, Token Token, Range Span)
        {
            public TokenSpan(Token token, Range span)
            : this(new(""), token, span)
            {}
        }

        [Union]
        public readonly partial struct Token : IUnion, IEquatable<Token>
        {
            public Token(Token.WhiteSpace ws)
            => Value = ws;
            public Token(Token.EOF eof)
            => Value = eof;
            public Token(Token.EOL eol)
            => Value = eol;

            public object Value { get; }

            public readonly record struct WhiteSpace(string Value)
            {
                public override string ToString()
                {
                    var sb = new StringBuilder();
                    foreach (var c in Value)
                        sb.Append(Escape([c]));
                    return $"\"{sb}\"";
                }
            }
            public readonly record struct EOL();
            public readonly record struct EOF();

            public override string ToString()
            => Value?.ToString()!;

            public override int GetHashCode()
            => Value?.GetHashCode() ?? -1;

            public static StringBuilder Escape(ReadOnlySpan<char> str)
            {
                var sb = new StringBuilder();
                foreach (var c in str)
                {
                    sb = c switch
                    {
                        '"' => sb.Append("\\\""),
                        '\\' => sb.Append("\\\\"),
                        '\t' => sb.Append("\\t"),
                        '\0' => sb.Append("\\0"),
                        '\r' => sb.Append("\\r"),
                        '\n' => sb.Append("\\n"),
                        _ => sb.Append(c),
                    };
                }
                return sb;
            }

            public static void Unescape(char c, StringBuilder sb)
            {
                _ = c switch
                {
                    '"' => sb.Append('"'),
                    '\\' => sb.Append('\\'),
                    't' => sb.Append('\t'),
                    '\0' => sb.Append('\0'),
                    '\r' => sb.Append('\r'),
                    '\n' => sb.Append('\n'),
                    _ => sb.Append(c),
                };
            }

            // public bool Equals(Token token)
            // => (this, token) switch
            // {
            //     (Token.WhiteSpace, Token.WhiteSpace { Value: null }) => true,
            //     (Token.WhiteSpace { Value: var vl }, Token.WhiteSpace { Value: var vr }) => vl == vr,
            //     (Token.WhiteSpace, _) => false,
            //     (Token.EOL, Token.EOL) => true,
            //     (Token.EOL, _) => false,
            //     (Token.EOF, Token.EOF) => true,
            //     (Token.EOF, _) => false,
            //     (null, _) => false,
            // };

            // public string TokenString()
            // => this switch
            // {
            //     Token.WhiteSpace { Value: string v } => v,
            //     Token.EOL => "\\n",
            //     Token.WhiteSpace or Token.EOF or null => "",
            // };

            public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Token r && Equals(r);

            public static bool operator ==(Token l, Token r)
            => l.Equals(r);

            public static bool operator !=(Token l, Token r)
            => !l.Equals(r);
        }
        """);
        Tokenizer.AppendLine($$"""
        #nullable enable
        namespace {{Namespace}}.Tokenize;

        [Serializable]
        public abstract class EBNFException : Exception
        {
            public abstract Range Range { get; }
            public abstract string ErrorCode { get; }
            public abstract string SubCategory { get; }
        }

        [Serializable]
        public abstract class TokenizerException(int pos) : EBNFException
        {
            public int Pos { get; } = pos;
            public override Range Range => Pos..Pos;
        }

        [Serializable]
        public class UnexpectedTokenizerException(int pos, char? unexpected) : TokenizerException(pos)
        {
            public char? Unexpected { get; } = unexpected;
            public override string ErrorCode => "EB_0001";
            public override string SubCategory => "Unexpected Token";

            public override string Message
            => $"Unexpected token ({(Unexpected is char unexpected ? Token.Escape([unexpected]) : "EOF")}) at pos: {Pos}";
        }

        [Serializable]
        public class ExpectedTokenizerException(int pos, char expected, char? actual) : TokenizerException(pos)
        {
            public char Expected { get; } = expected;
            public char? Actual { get; } = actual;
            public override string ErrorCode => "EB_0002";
            public override string SubCategory => "Expected Token";

            public override string Message
            => $"Expected token ({Token.Escape([Expected])}) but got ({(Actual is char actual ? Token.Escape([actual]) : "EOF")}) at pos: {Pos}";
        }

        public partial class Tokenizer(string input)
        {
            public Token CurrentToken => CurrentTokenSpan.Token;
            public Range CurrentSpan => CurrentTokenSpan.Span;
            public TokenSpan CurrentTokenSpan { get; private set => (PreviousTokenSpan, field) = (CurrentTokenSpan, value); } = new(new Token.WhiteSpace(""), 0..0);
            public Token PreviousToken => PreviousTokenSpan.Token;
            public Range PreviousSpan => PreviousTokenSpan.Span;
            public TokenSpan PreviousTokenSpan { get; private set; }
            private ReadOnlyMemory<char> _input = input.AsMemory();
            private int _i = 0;

            public void ScanToken()
            {
                var token = ScanTokenImpl(out var length) ?? throw new UnexpectedTokenizerException(_i, _input.First);
                var range = new Range(_i, _i += length);
                if (token is Token.WhiteSpace ws)
                {
                    token = ScanTokenImpl(out length) ?? throw new UnexpectedTokenizerException(_i, _input.First);
                    range = new Range(_i, _i += length);
                    CurrentTokenSpan = new(ws, token, range);
                }
                else
                    CurrentTokenSpan = new(token, range);
            }

            private partial Token? ScanTokenImpl(out int length);
        }
        """);
        TreePrintVisitor.AppendLine($$"""
        #nullable enable
        using {{Namespace}}.Parse;

        namespace {{Namespace}}.Visitors;

        public partial class TreePrintVisitor(ReadOnlyMemory<char> input) : Visitor
        {
            private int _depth = 0;
            private static readonly Dictionary<int, string> _indent = [];
            protected string IndentSpaces(int depth)
            {
                ref var indent = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
                if (exists)
                    return indent!;
                var s = (stackalloc char[depth * 2]);
                s.Fill(' ');
                return indent = new string(s);
            }
            protected void PrintTree(ReadOnlySpan<char> input, TreeNode node, bool isTerminal)
            => Console.WriteLine($"{IndentSpaces(_depth)}{node.GetType().Name} = [{node.Span}]{input[node.Span]}{(isTerminal ? "" : ":")}");
        """);
    }
    void IVisitor.Enter(Node node)
    {
        _isInNode = true;
        var isTypeSealed = node.Id.Operator.Token switch
        {
            Phases.Tokenize.Token.Symbol { Value: "?" } => true,
            Phases.Tokenize.Token.Symbol { Value: "*" } => false,
            _ => throw new UnreachableException(),
        };
        node.Accept(prettyPrintVisitor);
        var ebnf = OutputAndClear(prettyPrintVisitor, escapeXML: true);
        TreeNode.AppendLine($$"""

        /// <summary>
        /// <c>{{ebnf}}</c>
        /// </summary>
        """);
        TreeNode.Append($$"""public {{(isTypeSealed ? "sealed" : "abstract")}} partial record class {{IdToCSharp[(Primary)node.Id.Node]}}(""");
        foreach (var p in node.Params)
        {
            TreeNode.Append(ExpressionToType(p.Expressions[0]));
            TreeNode.Append($$""" {{IdToCSharp[(Primary)p.Expressions[1]]}}, """);
        }
        TreeNode.Append($$"""Range Span) : {{IdToCSharp[node.Inherit]}}(""");
        foreach (var a in node.Args)
            TreeNode.Append($$"""{{IdToCSharp[a]}}, """);
        TreeNode.Append("Span)");
        if (!isTypeSealed)
            TreeNode.AppendLine(";");
        else
        {
            TreeNode.AppendLine();
            TreeNode.AppendLine($$"""
            {
            """);
            if (!node.Params.IsEmpty)
            {
                TreeNode.AppendLine($$"""
                    public override void Accept(IVisitor visitor)
                    {
                """);
                var paramsCount = node.Params
                    .Select(static s => s.Expressions[0])
                    .Sum(static e => e switch
                    {
                        Postfix { Node: Primary { Name: not ("token-span" or "token") },  Operator.Token: Phases.Tokenize.Token.Symbol { Value: "*" } } => 2,
                        Postfix { Node: Primary { Name: not ("token-span" or "token") },  Operator.Token: Phases.Tokenize.Token.Symbol { Value: "?" } } or Primary { Name: not ("token-span" or "token") } => 1,
                        Primary or Postfix => 0,
                        _ => throw new UnreachableException(),
                    });
                switch (paramsCount)
                {
                    case 0:
                    {
                        IVisitor.AppendLine($$"""    void Visit({{IdToCSharp[(Primary)node.Id.Node]}} primary);""");
                        Visitor.AppendLine($$"""    public virtual void Visit({{IdToCSharp[(Primary)node.Id.Node]}} primary) {}""");
                        break;
                    }
                    case 1:
                    {
                        IVisitor.AppendLine($$"""    void Enter({{IdToCSharp[(Primary)node.Id.Node]}} primary);""");
                        Visitor.AppendLine($$"""    public virtual void Enter({{IdToCSharp[(Primary)node.Id.Node]}} primary) {}""");
                        IVisitor.AppendLine($$"""    void Exit({{IdToCSharp[(Primary)node.Id.Node]}} primary);""");
                        Visitor.AppendLine($$"""    public virtual void Exit({{IdToCSharp[(Primary)node.Id.Node]}} primary) {}""");
                        break;
                    }
                    case > 1:
                    {
                        IVisitor.AppendLine($$"""    void Enter({{IdToCSharp[(Primary)node.Id.Node]}} primary);""");
                        Visitor.AppendLine($$"""    public virtual void Enter({{IdToCSharp[(Primary)node.Id.Node]}} primary) {}""");
                        IVisitor.AppendLine($$"""    void Visit({{IdToCSharp[(Primary)node.Id.Node]}} primary);""");
                        Visitor.AppendLine($$"""    public virtual void Visit({{IdToCSharp[(Primary)node.Id.Node]}} primary) {}""");
                        IVisitor.AppendLine($$"""    void Exit({{IdToCSharp[(Primary)node.Id.Node]}} primary);""");
                        Visitor.AppendLine($$"""    public virtual void Exit({{IdToCSharp[(Primary)node.Id.Node]}} primary) {}""");
                        break;
                    }
                }
                if (paramsCount is not 0)
                    TreeNode.AppendLine($$"""
                            visitor.Enter(this);
                    """);
                var isFirst = true;
                foreach (var e in node.Params.Select(static s => s.Expressions))
                {
                    switch (e)
                    {
                        case [Postfix { Node: Primary { Name: not ("token-span" or "token") },  Operator.Token: Phases.Tokenize.Token.Symbol { Value: "*" } }, Primary prop]:
                        {
                            if (isFirst)
                                TreeNode.AppendLine($$"""
                                        for (var i = 0; i < {{IdToCSharp[prop]}}.Length; i++)
                                        {
                                            if (i > 0)
                                                visitor.Visit(this);
                                            {{IdToCSharp[prop]}}[i].Accept(visitor);
                                        }
                                """);
                            else
                                TreeNode.AppendLine($$"""
                                        for (var i = 0; i < {{IdToCSharp[prop]}}.Length; i++)
                                        {
                                            visitor.Visit(this);
                                            {{IdToCSharp[prop]}}[i].Accept(visitor);
                                        }
                                """);
                            break;
                        }
                        case [Postfix { Node: Primary { Name: not ("token-span" or "token") }, Operator.Token: Phases.Tokenize.Token.Symbol { Value: "?" } }, Primary prop]:
                        {
                            if (isFirst || paramsCount is not > 1)
                                TreeNode.AppendLine($$"""
                                        if ({{IdToCSharp[prop]}} is {} _node)
                                        {
                                            _node.Accept(visitor);
                                        }
                                """);
                            else
                                TreeNode.AppendLine($$"""
                                        if ({{IdToCSharp[prop]}} is {} _node)
                                        {
                                            visitor.Visit(this);
                                            _node.Accept(visitor);
                                        }
                                """);
                            break;
                        }
                        case [Primary { Name: not ("token-span" or "token") }, Primary prop]:
                        {
                            if (isFirst || paramsCount is not > 1)
                                TreeNode.AppendLine($$"""
                                        {{IdToCSharp[prop]}}.Accept(visitor);
                                """);
                            else
                                TreeNode.AppendLine($$"""
                                        visitor.Visit(this);
                                        {{IdToCSharp[prop]}}.Accept(visitor);
                                """);
                            break;
                        }
                        case [Primary or Postfix, Primary prop]:
                            break;
                        default: throw new UnreachableException();
                    }
                    isFirst = false;
                }

                if (paramsCount is not 0)
                    TreeNode.AppendLine($$"""
                            visitor.Exit(this);
                    """);
                TreeNode.AppendLine($$"""
                    }

                    public{{(isTypeSealed ? " " : " virtual ")}}bool Equals({{IdToCSharp[(Primary)node.Id.Node]}}? other)
                    => {{string.Join(" && ", node.Params.Select(s => s.Expressions switch
                    {
                        [Postfix { Operator.Token: Phases.Tokenize.Token.Symbol { Value: "*" }, Node: Primary }, Primary prop] => $"StructuralEquals({IdToCSharp[prop]}, other?.{IdToCSharp[prop]})",
                        [Postfix { Operator.Token: Phases.Tokenize.Token.Symbol { Value: "?" }, Node: Primary }, Primary prop] => $$"""(other?.{{IdToCSharp[prop]}}?.Equals({{IdToCSharp[prop]}}) ?? ({{IdToCSharp[prop]}}, other) is (null, { {{IdToCSharp[prop]}}: null }))""",
                        [Primary, Primary prop] => $"(other?.{IdToCSharp[prop]}.Equals({IdToCSharp[prop]}) ?? false)",
                        _ => throw new UnreachableException(),
                    }))}};

                    public override int GetHashCode()
                    => HashCode.Combine({{string.Join(", ", node.Params.Select(e => e is { Expressions: [_, Primary prop]} ? IdToCSharp[prop] : null))}});
                """);
                if (paramsCount is 0)
                    TreePrintVisitor.AppendLine($$"""

                        public override void Visit({{Namespace}}.Parse.{{IdToCSharp[(Primary)node.Id.Node]}} elem)
                        {
                            PrintTree(input.Span, elem, isTerminal: true);
                        }
                    """);
                else
                    TreePrintVisitor.AppendLine($$"""

                        public override void Enter({{Namespace}}.Parse.{{IdToCSharp[(Primary)node.Id.Node]}} elem)
                        {
                            PrintTree(input.Span, elem, isTerminal: false);
                            _depth++;
                        }

                        public override void Exit({{Namespace}}.Parse.{{IdToCSharp[(Primary)node.Id.Node]}} elem)
                        {
                            _depth--;
                        }
                    """);
            }
            TreeNode.AppendLine($$"""
            }
            """);
        }
    }
    void IVisitor.Exit(Node node)
    {
        _isInNode = false;
    }
    void IVisitor.Enter(Declaration declaration)
    {
        _isDeclarationBody = null;
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
    }
    void IVisitor.Visit(Declaration declaration)
    {
        _isDeclarationBody = _isDeclarationBody switch
        {
            null => false,
            false => true,
            true => true,
        };
    }
    void IVisitor.Enter(Choice choice)
    {
        PrintTreeDocumentation(choice, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Choice choice)
    {
        _depth--;
    }
    void IVisitor.Enter(Sequence sequence)
    {
        PrintTreeDocumentation(sequence, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Sequence sequence)
    {
        _depth--;
    }
    void IVisitor.Enter(Postfix postfix)
    {
        PrintTreeDocumentation(postfix, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Postfix postfix)
    {
        _depth--;
    }
    void IVisitor.Visit(Primary primary)
    {
        if (_isDeclarationBody is true)
            PrintTreeDocumentation(primary, isTerminal: true);
    }
    void IVisitor.Exit(Declaration declaration)
    {
        _isDeclarationBody = null;
        Parser.AppendLine($$"""
            /// var end = tokenizer.CurrentSpan.End;
            /// </code>
            /// </remarks>
            private partial {{ExpressionToType(declaration.Node)}} Parse_{{IdToCSharp[declaration.Id]}}(Tokenizer tokenizer);

        """);
    }

    void IVisitor.Exit(Phases.Parse.File file)
    {
        IVisitor.AppendLine("}");
        Visitor.AppendLine("}");
        Parser.AppendLine($$"""
            protected static class Helper
            {
                public static ImmutableArray<TAny> ParseAny<TAny>(Func<Tokenizer, TAny> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
                where TAny : TreeNode
                {
                    return [.. ParseNodes(parser, tokenizer, endOfParse)];

                    static IEnumerable<TAny> ParseNodes(Func<Tokenizer, TAny> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
                    {
                        while (!endOfParse(tokenizer.CurrentTokenSpan))
                            yield return parser(tokenizer);
                    }
                }

                public static ImmutableArray<Token> ParseAny(Token token, Tokenizer tokenizer)
                {
                    return [.. ParseTokens(token, tokenizer)];

                    static IEnumerable<Token> ParseTokens(Token token, Tokenizer tokenizer)
                    {
                        Expect(tokenizer, token, out var ts);
                        yield return ts.Token;
                        while (TryConsume(tokenizer, token, out ts))
                            yield return ts.Token;
                    }
                }

                public static ImmutableArray<TMultiple> ParseMultiple<TMultiple>(Func<Tokenizer, TMultiple> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
                where TMultiple : TreeNode
                {
                    return [.. ParseNodes(parser, tokenizer, endOfParse)];

                    static IEnumerable<TMultiple> ParseNodes(Func<Tokenizer, TMultiple> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
                    {
                        while (!endOfParse(tokenizer.CurrentTokenSpan))
                            yield return parser(tokenizer);
                    }
                }

                public static ImmutableArray<Token> ParseMultiple(Token token, Tokenizer tokenizer)
                {
                    return [.. ParseTokens(token, tokenizer)];

                    static IEnumerable<Token> ParseTokens(Token token, Tokenizer tokenizer)
                    {
                        Expect(tokenizer, token, out var ts);
                        yield return ts.Token;
                        while (TryConsume(tokenizer, token, out ts))
                            yield return ts.Token;
                    }
                }

                public static void Expect(Tokenizer tokenizer, Token token)
                => Expect(tokenizer, token, out _);

                public static void Expect(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
                {
                    if (!TryConsume(tokenizer, token, out tokenSpan))
                        throw new ParserExpectedException(tokenizer.CurrentTokenSpan, token);
                }

                public static bool TryConsume(Tokenizer tokenizer, Token token)
                => TryConsume(tokenizer, token, out _);

                public static bool TryConsume(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
                {
                    if (tokenizer.CurrentToken != token)
                    {
                        tokenSpan = default;
                        return false;
                    }
                    tokenSpan = tokenizer.CurrentTokenSpan;
                    tokenizer.ScanToken();
                    return true;
                }
            }
        }

        [Serializable]
        public abstract class ParserException(TokenSpan tokenSpan) : EBNFException
        {
            public TokenSpan TokenSpan { get; } = tokenSpan;
            public override Range Range => TokenSpan.Span;
        }

        [Serializable]
        public class ParserUnexpectedException(TokenSpan tokenSpan) : ParserException(tokenSpan)
        {
            public override string ErrorCode => "EB_0003";
            public override string SubCategory => "Unexpected Parser";
            public override string Message
            => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}";
        }

        [Serializable]
        public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
        {
            public Token Expected { get; } = expected;
            public override string ErrorCode => "EB_0004";
            public override string SubCategory => "Expected Parser";

            public override string Message
            => $"Expected token {Expected} but got ({TokenSpan.Token}) at pos: {TokenSpan.Span}";
        }
        """);
        TreePrintVisitor.AppendLine($$"""
        }
        """);
    }

    private string ExpressionToType(Expression expression)
    => expression switch
    {
        Postfix { Operator.Token: Phases.Tokenize.Token.Symbol { Value: "*" }, Node: Primary node } => $"{typeof(ImmutableArray).FullName}<{IdToCSharp[node]}>",
        Postfix { Operator.Token: Phases.Tokenize.Token.Symbol { Value: "?" }, Node: Primary node } => $"{IdToCSharp[node]}?",
        Primary node => IdToCSharp[node],
        _ => throw new UnreachableException(),
    };

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

    private sealed class IdToCSharpVisitor : IVisitor
    {
        public Dictionary<Primary, string> Names = [];

        void IVisitor.Visit(Primary primary)
        {
            if (primary is not { TokenSpan.Token: Phases.Tokenize.Token.Id, Name: var id })
                return;
            ref var name = ref CollectionsMarshal.GetValueRefOrAddDefault(Names, primary, out var exists);
            if (exists)
                return;
            name = string.Create(id.Length - id.Count(c => c is '-'), id.AsSpan(), (c, s) =>
            {
                c[0] = char.ToUpperInvariant(s[0]);
                c = c[1..];
                s = s[1..];
                while (!c.IsEmpty)
                {
                    if (s[0] is '-')
                    {
                        s = s[1..];
                        c[0] = char.ToUpperInvariant(s[0]);
                    }
                    else
                        c[0] = s[0];
                    c = c[1..];
                    s = s[1..];
                }
            });
        }
    }
}
