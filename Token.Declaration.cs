using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;


namespace RecursiveParsing;

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

    public override bool Equals([NotNullWhen(true)] object? obj)
    => obj is Token r && Equals(r);

    public static bool operator ==(Token l, Token r)
    => l.Equals(r);

    public static bool operator !=(Token l, Token r)
    => !l.Equals(r);
}
