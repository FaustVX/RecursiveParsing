namespace RecursiveParsing;

[Serializable]
public class TokenizerException(int pos, char @char) : Exception
{
    public int Pos { get; } = pos;
    public char Char { get; } = @char;

    public override string ToString()
    {
        Console.Error.WriteLine($"Unexpected token ({Char}) at pos: {Pos}");
        return base.ToString();
    }
}

public class Tokenizer(string input)
{
    public Token NextToken => NextTokenSpan.Token;
    public Range NextSpan => NextTokenSpan.Span;
    public TokenSpan NextTokenSpan { get; private set; } = new(new Token.WhiteSpace(""), 0..0);
    private ReadOnlyMemory<char> _input = input.AsMemory();
    private int _i = 0;

    public void Expect(Token token)
    {
        if (NextToken != token)
            throw new ParserExpectedException(NextTokenSpan, token);
        ScanToken();
    }

    public bool TryConsume(Token token)
    {
        if (NextToken != token)
            return false;
        ScanToken();
        return true;
    }

    public void ScanToken()
    {
        var token = ScanTokenImpl(out var length) ?? throw new TokenizerException(_i, _input.First ?? '\0');
        var range = new Range(_i, _i += length);
        if (token is Token.WhiteSpace ws)
        {
            token = ScanTokenImpl(out length) ?? throw new TokenizerException(_i, _input.First ?? '\0');
            range = new Range(_i, _i += length);
            NextTokenSpan = new(ws, token, range);
        }
        else
            NextTokenSpan = new(new(""), token, range);
    }

    private Token? ScanTokenImpl(out int length)
    {
        switch (_input.First)
        {
            case null: // End of line
                length = 0;
                return new Token.EOL();
            case char ws when char.IsWhiteSpace(ws): // whitespace
            {
                var input = _input;
                length = 0;
                do
                {
                    length++;
                    _input++;
                } while (_input.First is char c && char.IsWhiteSpace(c));
                return new Token.WhiteSpace(input[..length].ToString());
            }
            case ('<' or '>' or '=' or '!') and var symbol: // 2-symbol
                _input++;
                if (_input.First is '=' and var equals)
                {
                    _input++;
                    length = 2;
                    return new Token.Symbol($"{symbol}{equals}");
                }
                length = 1;
                return new Token.Symbol(symbol);
            case ('+' or '-' or '*' or '/' or '^' or '(' or ')' or ',' or '?' or ':' or ';' or '{' or '}') and var symbol: // single symbol
                _input++;
                length = 1;
                return new Token.Symbol(symbol);
            case >= '0' and <= '9': // digit
            {
                length = 0;
                int i = 0;
                do
                {
                    if (_input.First is not '_')
                        i = i * 10 + _input.First!.Value - '0';
                    length++;
                    _input++;
                } while (_input.First is (>= '0' and <= '9') or '_');
                return new Token.Int(i);
            }
            case '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'): // identifier
            {
                var input = _input;
                length = 0;
                do
                {
                    length++;
                    _input++;
                } while (_input.First is '_' or (>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'));
                return new Token.Id(input[..length].ToString());
            }
        }
        length = 0;
        return null;
    }
}
