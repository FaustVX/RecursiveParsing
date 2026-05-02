using System.Text;

namespace RecursiveParsing;

public partial class Tokenizer
{
    private partial Token? ScanTokenImpl(out int length)
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
                return new Token.Symbol(symbol.ToString());
            case ('+' or '-' or '*' or '/' or '^' or '(' or ')' or ',' or '?' or ':' or ';' or '{' or '}') and var symbol: // single symbol
                _input++;
                length = 1;
                return new Token.Symbol(symbol.ToString());
            case '"': // string
            {
                length = 1;
                _input += length;
                var sb = new StringBuilder();
                while (_input.First is not '"')
                {
                    if (_input.IsEmpty)
                        throw new ExpectedTokenizerException(_i + length, '"', _input.First);
                    if (_input.First is '\\') // escaped
                    {
                        length++;
                        _input++;
                        if (_input.First is not ('"' or '\\' or 'r' or 'n' or 't' or '0'))
                            throw new UnexpectedTokenizerException(_i + length, _input.First ?? '\0');
                        Token.Unescape(_input.First!.Value, sb);
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
