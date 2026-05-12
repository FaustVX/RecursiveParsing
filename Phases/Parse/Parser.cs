using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Phases.Parse;

[Serializable]
public abstract class ParserException(TokenSpan tokenSpan) : EBNFException
{
    public TokenSpan TokenSpan { get; } = tokenSpan;
    public override Range Range => TokenSpan.Span;
}

[Serializable]
public class ParserUnexpectedException(TokenSpan tokenSpan) : ParserException(tokenSpan)
{
    public override string ErrorCode => "EB_0003";
    public override string SubCategory => "Unexpected Parser";
    public override string Message
    => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}";
}

[Serializable]
public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
{
    public Token Expected { get; } = expected;
    public override string ErrorCode => "EB_0004";
    public override string SubCategory => "Expected Parser";

    public override string Message
    => $"Expected token {Expected} but got ({TokenSpan.Token}) at pos: {TokenSpan.Span}";
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
            catch (EBNFException ex)
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
            catch (EBNFException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

    private static ImmutableArray<TAny> ParseAny<TAny>(Func<Tokenizer, TAny> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
    where TAny : TreeNode
    {
        return [.. ParseNodes(parser, tokenizer, endOfParse)];

        static IEnumerable<TAny> ParseNodes(Func<Tokenizer, TAny> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
        {
            while (!endOfParse(tokenizer.CurrentTokenSpan))
                yield return parser(tokenizer);
        }
    }

    private static ImmutableArray<Token> ParseAny(Token token, Tokenizer tokenizer)
    {
        return [.. ParseTokens(token, tokenizer)];

        static IEnumerable<Token> ParseTokens(Token token, Tokenizer tokenizer)
        {
            Expect(tokenizer, token, out var ts);
            yield return ts.Token;
            while (TryConsume(tokenizer, token, out ts))
                yield return ts.Token;
        }
    }

    private static ImmutableArray<TMultiple> ParseMultiple<TMultiple>(Func<Tokenizer, TMultiple> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
    where TMultiple : TreeNode
    {
        return [.. ParseNodes(parser, tokenizer, endOfParse)];

        static IEnumerable<TMultiple> ParseNodes(Func<Tokenizer, TMultiple> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
        {
            while (!endOfParse(tokenizer.CurrentTokenSpan))
                yield return parser(tokenizer);
        }
    }

    private static ImmutableArray<Token> ParseMultiple(Token token, Tokenizer tokenizer)
    {
        return [.. ParseTokens(token, tokenizer)];

        static IEnumerable<Token> ParseTokens(Token token, Tokenizer tokenizer)
        {
            Expect(tokenizer, token, out var ts);
            yield return ts.Token;
            while (TryConsume(tokenizer, token, out ts))
                yield return ts.Token;
        }
    }

    public static void Expect(Tokenizer tokenizer, Token token)
    => Expect(tokenizer, token, out _);

    public static void Expect(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
    {
        if (!TryConsume(tokenizer, token, out tokenSpan))
            throw new ParserExpectedException(tokenizer.CurrentTokenSpan, token);
    }

    public static bool TryConsume(Tokenizer tokenizer, Token token)
    => TryConsume(tokenizer, token, out _);

    public static bool TryConsume(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
    {
        if (tokenizer.CurrentToken != token)
        {
            tokenSpan = default;
            return false;
        }
        tokenSpan = tokenizer.CurrentTokenSpan;
        tokenizer.ScanToken();
        return true;
    }
}
