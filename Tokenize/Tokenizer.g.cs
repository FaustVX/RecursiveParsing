#nullable enable
namespace RecursiveParsing.Tokenize;

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
