using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace EBNFParser.Phases.Tokenize;

public readonly record struct TokenSpan(Token.WhiteSpace Before, Token Token, Range Span)
{
    public TokenSpan(Token token, Range span)
    : this(new(""), token, span)
    {}
}

public readonly partial struct Token
{
    public Token(Token.Id id)
    => Value = id;
    public Token(Token.Terminal t)
    => Value = t;
    public Token(Token.Symbol s)
    => Value = s;
    public Token(Token.String s)
    => Value = s;
    public readonly record struct Id(string Value);
    public readonly record struct Terminal(string Value);
    public readonly record struct String(string Value)
    {

        public override string ToString()
        {
            var sb = new StringBuilder($$"""{{nameof(Token.String)}} { Value = """).Append('"');
            foreach (var c in Value)
                sb.Append(Escape([c]));
            return sb.Append('"').Append(" }").ToString();
        }
    }

    public readonly record struct Symbol(string Value);
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

    public bool Equals(Token token)
    => (this, token) switch
    {
        (Token.Id, Token.Id { Value: null }) => true,
        (Token.Id { Value: var vl }, Token.Id { Value: var vr }) => vl == vr,
        (Token.Id, _) => false,
        (Token.Terminal, Token.Terminal { Value: null }) => true,
        (Token.Terminal { Value: var vl }, Token.Terminal { Value: var vr }) => vl == vr,
        (Token.Terminal, _) => false,
        (Token.Symbol, Token.Symbol { Value: null }) => true,
        (Token.Symbol { Value: string vl }, Token.Symbol { Value: string vr }) => vl == vr,
        (Token.Symbol, Token.Symbol) => false,
        (Token.Symbol, _) => false,
        (Token.String, Token.String { Value: null }) => true,
        (Token.String { Value: var vl }, Token.String { Value: var vr }) => vl == vr,
        (Token.String, _) => false,
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
        Token.Id { Value: string v } => v,
        Token.Terminal { Value: string v } => v,
        Token.Symbol { Value: string v } => v,
        Token.String { Value: string v } => $"\"{v}\"",
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
