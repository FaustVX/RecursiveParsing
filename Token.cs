using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RecursiveParsing;

public readonly record struct TokenSpan(Token.WhiteSpace Before, Token Token, Range Span)
{
    public TokenSpan(Token token, Range span)
    : this(new(""), token, span)
    {}
}

public readonly union Token(Token.WhiteSpace, Token.Int, Token.Id, Token.Symbol, Token.String, Token.EOL) : IEquatable<Token>
{
    public readonly record struct WhiteSpace(string Value)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var c in Value)
                sb.Append(Escape(c));
            return $"\"{sb}\"";
        }

        private static string Escape(char c)
        => c switch
        {
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\0' => "\\0",
            _ => c.ToString(),
        };
    }
    public readonly record struct Int(int Value);
    public readonly record struct Id(string Value);
    public readonly record struct String(string Value);
    public readonly record struct Symbol(Symbol.CharOrString Value)
    {
        public readonly union CharOrString(char, string);

        public override string ToString()
        => $"{nameof(Symbol)} {{ Value = {Value.Value?.ToString()} }}";
    }
    public readonly record struct EOL();

    public override string ToString()
    => Value?.ToString()!;

    public override int GetHashCode()
    => Value?.GetHashCode() ?? -1;

    public bool Equals(Token token)
    => (this, token) switch
    {
        (Token.WhiteSpace { Value: var vl }, Token.WhiteSpace { Value: var vr }) => vl == vr,
        (Token.Int { Value: var vl }, Token.Int { Value: var vr }) => vl == vr,
        (Token.Id { Value: var vl }, Token.Id { Value: var vr }) => vl == vr,
        (Token.Symbol { Value: string vl }, Token.Symbol { Value: string vr }) => vl == vr,
        (Token.Symbol { Value: char vl }, Token.Symbol { Value: char vr }) => vl == vr,
        (Token.EOL, Token.EOL) => true,
        _ => false,
    };

    public override bool Equals([NotNullWhen(true)] object? obj)
    => obj is Token r && Equals(r);

    public static bool operator ==(Token l, Token r)
    => l.Equals(r);

    public static bool operator !=(Token l, Token r)
    => !l.Equals(r);
}
