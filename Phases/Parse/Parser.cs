using System.Runtime.CompilerServices;
using RecursiveParsing.Phases.Tokenize;

namespace RecursiveParsing.Phases.Parse;

[Serializable]
public abstract class ParserException(TokenSpan tokenSpan) : Exception
{
    public TokenSpan TokenSpan { get; } = tokenSpan;
}

[Serializable]
public class ParserUnexpectedException(TokenSpan tokenSpan) : ParserException(tokenSpan)
{
    public override string ToString()
    => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
}

[Serializable]
public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
{
    public Token Expected { get; } = expected;

    public override string ToString()
    => $"Expected token {Expected} but got ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
}

public partial class Parser(string input)
{
    public Tokenizer Tokenizer { get; } = new(input);

    /// <summary>
    /// • file                      := declaration*
    /// <br/>
    /// • declaration               := ID ":=" expression EOL+
    /// <br/>
    /// </summary>
    public File ParseFile()
    {
        Tokenizer.ScanToken();
        var tree = Parse(Tokenizer);
        Expect(Tokenizer, new Token.EOF());
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
    public Expression ParseExpression()
    {
        Tokenizer.ScanToken();
        var tree = Parse(Tokenizer);
        Expect(Tokenizer, new Token.EOF());
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

    public static void Expect(Tokenizer tokenizer, Token token)
    => Expect(tokenizer, token, out _);

    public static void Expect(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
    {
        tokenSpan = tokenizer.NextTokenSpan;
        if (tokenizer.NextToken != token)
            throw new ParserExpectedException(tokenizer.NextTokenSpan, token);
        tokenizer.ScanToken();
    }

    public static bool TryConsume(Tokenizer tokenizer, Token token)
    => TryConsume(tokenizer, token, out _);

    public static bool TryConsume(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
    {
        tokenSpan = tokenizer.NextTokenSpan;
        if (tokenizer.NextToken != token)
            return false;
        tokenizer.ScanToken();
        return true;
    }
}
