namespace RecursiveParsing;

public struct Tokenizer(string input)
{
    public Token? NextToken { get; private set; }
    public ReadOnlyMemory<char> Input { get; private set; } = input.AsMemory();

    public void ScanToken()
    => NextToken = ScanTokenImpl();

    private Token? ScanTokenImpl()
    {
        switch (Input.First)
        {
            case null:
                return new Token.EOL();
            case ' ':
                Input++;
                return ScanTokenImpl();
            case ('+' or '-' or '*' or '/' or '^' or '(' or ')') and var symbol:
                Input++;
                return new Token.Symbol(symbol);
            case >= '0' and <= '9':
            {
                int i = 0;
                do
                {
                    i = i * 10 + Input.First!.Value - '0';
                    Input++;
                } while (Input.First is >= '0' and <= '9');
                return new Token.Int(i);
            }
            case '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
            {
                var input = Input;
                int i = 0;
                do
                {
                    i++;
                    Input++;
                } while (Input.First is '_' or (>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'));
                return new Token.Id(input[..i].ToString());
            }
        }
        return null;
    }
}
