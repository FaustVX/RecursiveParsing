using System.Text;


namespace RecursiveParsing;

public readonly partial struct Token
{
    public Token(Token.Int i)
    => Value = i;
    public Token(Token.Id i)
    => Value = i;
    public Token(Token.String s)
    => Value = s;
    public Token(Token.Symbol s)
    => Value = s;

    public readonly record struct Int(int Value);
    public readonly record struct Id(string Value);
    public readonly record struct String(string Value)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var c in Value)
                sb.Append(Token.Escape([c]));
            return $"\"{sb}\"";
        }
    }
    public readonly record struct Symbol(string Value)
    {
        public override string ToString()
        => $"{nameof(Symbol)} {{ Value = {Value} }}";
    }

    public bool Equals(Token token)
    => (this, token) switch
    {
        (Token.WhiteSpace, Token.WhiteSpace { Value: null }) => true,
        (Token.WhiteSpace { Value: var vl }, Token.WhiteSpace { Value: var vr }) => vl == vr,
        (Token.WhiteSpace, _) => false,
        // (Token.Int, Token.Int { Value: null }) => true,
        (Token.Int { Value: var vl }, Token.Int { Value: var vr }) => vl == vr,
        (Token.Int, _) => false,
        (Token.Id, Token.Id { Value: null }) => true,
        (Token.Id { Value: var vl }, Token.Id { Value: var vr }) => vl == vr,
        (Token.Id, _) => false,
        (Token.String, Token.String { Value: null }) => true,
        (Token.String { Value: var vl }, Token.String { Value: var vr }) => vl == vr,
        (Token.String, _) => false,
        (Token.Symbol, Token.Symbol { Value: null }) => true,
        (Token.Symbol { Value: var vl }, Token.Symbol { Value: var vr }) => vl == vr,
        (Token.Symbol, _) => false,
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
        Token.Int { Value: int v } => v.ToString(),
        Token.Id { Value: string v } => v,
        Token.String { Value: string v } => v,
        Token.Symbol { Value: string v } => v,
        Token.EOL => "\\n",
        Token.WhiteSpace or Token.EOF or null => "",
    };
}
