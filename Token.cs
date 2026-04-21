namespace RecursiveParsing;

public readonly union Token(Token.Int, Token.Id, Token.Symbol, Token.EOL)
{
    public readonly record struct Int(int Value);
    public readonly record struct Id(string Value);
    public readonly record struct Symbol(char Value);
    public readonly record struct EOL();
}
