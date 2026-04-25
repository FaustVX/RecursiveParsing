using System.Text;

namespace RecursiveParsing;

[Serializable]
public abstract class TokenizerException(int pos) : Exception
{
    public int Pos { get; } = pos;
}

[Serializable]
public class UnexpectedTokenizerException(int pos, char unexpected) : TokenizerException(pos)
{
    public char Unexpected { get; } = unexpected;

    public override string ToString()
    => $"Unexpected token ({Unexpected}) at pos: {Pos}\n" + base.ToString();
}

[Serializable]
public class ExpectedTokenizerException(int pos, char expected, string actual) : TokenizerException(pos)
{
    public char Expected { get; } = expected;
    public string Actual { get; } = actual;

    public override string ToString()
    => $"Expected token ({Expected}) but got ({Actual}) at pos: {Pos}\n" + base.ToString();
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
        var token = ScanTokenImpl(out var length) ?? throw new UnexpectedTokenizerException(_i, _input.First ?? '\0');
        var range = new Range(_i, _i += length);
        if (token is Token.WhiteSpace ws)
        {
            token = ScanTokenImpl(out length) ?? throw new UnexpectedTokenizerException(_i, _input.First ?? '\0');
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
            case null: // End of file
                length = 0;
                return new Token.EOF();
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
            case '"': // string
            {
                length = 1;
                _input += length;
                var sb = new StringBuilder();
                while (_input.First is not '"')
                {
                    if (_input.IsEmpty || _input.First is '\r' or '\n')
                        throw new ExpectedTokenizerException(_i + length, '"', Token.String.Escape([_input.First ?? '\0']).ToString());
                    if (_input.First is '\\') // escaped
                    {
                        length++;
                        _input++;
                        if (_input.First is not ('"' or '\\' or 'r' or 'n' or 't' or '0'))
                            throw new UnexpectedTokenizerException(_i + length, _input.First ?? '\0');
                        Token.String.Unescape(_input.First!.Value, sb);
                    }
                    else
                        sb.Append(_input.First!.Value);
                    length++;
                    _input++;
                }
                length++;
                _input++;
                return new Token.String(sb.ToString());
            }
            case >= '0' and <= '9': // number
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
