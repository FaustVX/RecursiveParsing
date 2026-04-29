using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EBNFParser.Phases.Tokenize;

public readonly record struct TokenSpan(Token.WhiteSpace Before, Token Token, Range Span)
{
    public TokenSpan(Token token, Range span)
    : this(new(""), token, span)
    {}
}

public readonly union Token(Token.WhiteSpace, Token.Id, Token.Terminal, Token.Symbol, Token.String, Token.EOL, Token.EOF) : IEquatable<Token>
{
    public readonly record struct WhiteSpace(string Value)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var c in Value)
                sb.Append(Token.String.Escape([c]));
            return $"\"{sb}\"";
        }
    }
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
    }
    public readonly record struct Symbol(string Value);
    public readonly record struct EOL();
    public readonly record struct EOF();

    public override string ToString()
    => Value?.ToString()!;

    public override int GetHashCode()
    => Value?.GetHashCode() ?? -1;

    public bool Equals(Token token)
    => (this, token) switch
    {
        (Token.WhiteSpace, Token.WhiteSpace { Value: null }) => true,
        (Token.WhiteSpace, not Token.WhiteSpace) => false,
        (Token.WhiteSpace { Value: var vl }, Token.WhiteSpace { Value: var vr }) => vl == vr,
        (Token.Id, Token.Id { Value: null }) => true,
        (Token.Id, not Token.Id) => false,
        (Token.Id { Value: var vl }, Token.Id { Value: var vr }) => vl == vr,
        (Token.Terminal, Token.Terminal { Value: null }) => true,
        (Token.Terminal, not Token.Terminal) => false,
        (Token.Terminal { Value: var vl }, Token.Terminal { Value: var vr }) => vl == vr,
        (Token.Symbol, Token.Symbol { Value: null }) => true,
        (Token.Symbol, not Token.Symbol) => false,
        (Token.Symbol { Value: string vl }, Token.Symbol { Value: string vr }) => vl == vr,
        (Token.Symbol, Token.Symbol) => false,
        (Token.String, Token.String { Value: null }) => true,
        (Token.String, not Token.String) => false,
        (Token.String { Value: var vl }, Token.String { Value: var vr }) => vl == vr,
        (Token.EOL, not Token.EOL) => false,
        (Token.EOL, Token.EOL) => true,
        (Token.EOF, not Token.EOF) => false,
        (Token.EOF, Token.EOF) => true,
        (null, _) => false,
    };

    public string TokenString()
    => this switch
    {
        Token.WhiteSpace { Value: string v } => v,
        Token.Id { Value: string v } => v,
        Token.Terminal { Value: string v } => v,
        Token.Symbol { Value: string v } => v,
        Token.String { Value: string v } => $"\"{v}\"",
        Token.EOL => "\\n",
        Token.WhiteSpace or Token.Id or Token.Terminal or Token.Symbol or Token.String or Token.EOF or null => "",
    };

    public override bool Equals([NotNullWhen(true)] object? obj)
    => obj is Token r && Equals(r);

    public static bool operator ==(Token l, Token r)
    => l.Equals(r);

    public static bool operator !=(Token l, Token r)
    => !l.Equals(r);
}
