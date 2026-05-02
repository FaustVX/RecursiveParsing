using System.Text;


namespace RecursiveParsing;

[Serializable]
public abstract class TokenizerException(int pos) : Exception
{
    public int Pos { get; } = pos;
}

[Serializable]
public class UnexpectedTokenizerException(int pos, char? unexpected) : TokenizerException(pos)
{
    public char? Unexpected { get; } = unexpected;

    public override string ToString()
    => $"Unexpected token ({(Unexpected is char unexpected ? Token.Escape([unexpected]) : "EOF")}) at pos: {Pos}\n" + base.ToString();
}

[Serializable]
public class ExpectedTokenizerException(int pos, char expected, char? actual) : TokenizerException(pos)
{
    public char Expected { get; } = expected;
    public char? Actual { get; } = actual;

    public override string ToString()
    => $"Expected token ({Token.Escape([Expected])}) but got ({(Actual is char actual ? Token.Escape([actual]) : "EOF")}) at pos: {Pos}\n" + base.ToString();
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
