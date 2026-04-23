using System.Text;

namespace RecursiveParsing;

public readonly record struct TokenSpan(Token.WhiteSpace Before, Token Token, Range Span)
{
    public TokenSpan(Token token, Range span)
    : this(new(""), token, span)
    {}
}

public readonly union Token(Token.WhiteSpace, Token.Int, Token.Id, Token.Symbol, Token.EOL)
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
    public readonly record struct Symbol(Symbol.CharOrString Value)
    {
        public readonly union CharOrString(char, string);

        public override string ToString()
        => $"{nameof(Symbol)} {{ Value = {Value.Value?.ToString()} }}";
    }
    public readonly record struct EOL();

    public override string ToString()
    => Value?.ToString()!;
}
