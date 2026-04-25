using System.Runtime.CompilerServices;

namespace RecursiveParsing;

[Serializable]
public class ParserException(TokenSpan tokenSpan) : Exception
{
    public TokenSpan TokenSpan { get; } = tokenSpan;

    public override string ToString()
    => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
}

[Serializable]
public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
{
    public Token Expected { get; } = expected;

    public override string ToString()
    => $"Expected token {Expected}\n" + base.ToString();
}

public partial class Parser
{
    /// <summary>
    /// • file                      := declaration*
    /// <br/>
    /// • declaration               := ID ":=" expression EOL+
    /// <br/>
    /// </summary>
    public File ParseFile(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = Parse(tokenizer);
        tokenizer.Expect(new Token.EOF());
        return tree;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        File Parse(Tokenizer tokenizer)
        {
            try
            {
                return ParseFile(tokenizer);
            }
            catch (ParserException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

    /// <summary>
    /// • expression                := choice
    /// <br/>
    /// • choice                    := sequence ("|" sequence)*
    /// <br/>
    /// • sequence                  := postfix+
    /// <br/>
    /// • postfix                   := primary ("?" | "+" | "*")?
    /// <br/>
    /// • primary                   := ID | TERMINAL | STRING | "(" expression ")"
    /// </summary>
    public Expression ParseExpression(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = Parse(tokenizer);
        tokenizer.Expect(new Token.EOF());
        return tree;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Expression Parse(Tokenizer tokenizer)
        {
            try
            {
                return ParseExpression(tokenizer);
            }
            catch (ParserException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

}
