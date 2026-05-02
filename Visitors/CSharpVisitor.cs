using System.Runtime.CompilerServices;
using System.Text;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class CSharpVisitor(string @namespace, string parserClass) : IVisitor
{
    public StringBuilder Parser { get; } = new();
    public StringBuilder IVisitor { get; } = new();
    public StringBuilder TreeNode { get; } = new();
    public StringBuilder Token { get; } = new();
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
        using System.Collections.Immutable;
        using EBNFParser.Phases.Tokenize;

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
        Token.AppendLine($$"""
        namespace {{Namespace}};

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

            public bool Equals(Token token)
            => (this, token) switch
            {
                (Token.WhiteSpace, Token.WhiteSpace { Value: null }) => true,
                (Token.WhiteSpace { Value: var vl }, Token.WhiteSpace { Value: var vr }) => vl == vr,
                (Token.WhiteSpace, _) => false,
                (Token.EOL, Token.EOL) => true,
                (Token.EOL, _) => false,
                (Token.EOF, Token.EOF) => true,
                (Token.EOF, _) => false,
                (null, _) => false,
            };

            public string TokenString()
            => this switch
            {
                Token.WhiteSpace { Value: string v } => v,
                Token.EOL => "\\n",
                Token.WhiteSpace or Token.EOF or null => "",
            };

            public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is Token r && Equals(r);

            public static bool operator ==(Token l, Token r)
            => l.Equals(r);

            public static bool operator !=(Token l, Token r)
            => !l.Equals(r);
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
                    tokenSpan = tokenizer.CurrentTokenSpan;
                    if (tokenizer.CurrentToken != token)
                        throw new ParserExpectedException(tokenizer.CurrentTokenSpan, token);
                    tokenizer.ScanToken();
                }

                public static bool TryConsume(Tokenizer tokenizer, Token token)
                => TryConsume(tokenizer, token, out _);

                public static bool TryConsume(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
                {
                    tokenSpan = tokenizer.CurrentTokenSpan;
                    if (tokenizer.CurrentToken != token)
                        return false;
                    tokenizer.ScanToken();
                    return true;
                }
            }
        }

        [Serializable]
        public abstract class ParserException(TokenSpan tokenSpan) : Exception
        {
            public TokenSpan TokenSpan { get; } = tokenSpan;
        }

        [Serializable]
        public class ParserUnexpectedException(TokenSpan tokenSpan) : ParserException(tokenSpan)
        {
            public override string ToString()
            => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
        }

        [Serializable]
        public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
        {
            public Token Expected { get; } = expected;

            public override string ToString()
            => $"Expected token {Expected} but got ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
        }
        """);
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
