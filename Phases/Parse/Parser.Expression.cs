using System.Collections.Immutable;
using RecursiveParsing.Phases.Tokenize;

namespace RecursiveParsing.Phases.Parse;
public partial class Parser
{
    /// <summary>
    /// expression := choice
    /// </summary>
    private Expression ParseExpression(Tokenizer tokenizer)
    => ParseChoice(tokenizer);

    /// <summary>
    /// choice := sequence ("|" sequence)*
    /// </summary>
    private Expression ParseChoice(Tokenizer tokenizer)
    {
        var choices = ParseSequences(tokenizer).ToImmutableArray();
        if (choices is [])
            throw new ParserUnexpectedException(default);
        if (choices is [var expr])
            return expr;
        return new Choice(choices);

        IEnumerable<Expression> ParseSequences(Tokenizer tokenizer)
        {
            yield return ParseSequence(tokenizer);
            while (TryConsume(tokenizer, new Token.Symbol { Value = '|' }))
                yield return ParseSequence(tokenizer);
        }
    }

    /// <summary>
    /// sequence := postfix+
    /// </summary>
    private Expression ParseSequence(Tokenizer tokenizer)
    {
        var sequences = ParsePostfixes(tokenizer).ToImmutableArray();
        if (sequences is [])
            throw new ParserUnexpectedException(default);
        if (sequences is [var expr])
            return expr;
        return new Sequence(sequences);

        IEnumerable<Expression> ParsePostfixes(Tokenizer tokenizer)
        {
            yield return ParsePostfix(tokenizer);
            while (tokenizer.CurrentToken is Token.Id or Token.Terminal or Token.String or Token.Symbol { Value: '(' })
                yield return ParsePostfix(tokenizer);
        }
    }

    /// <summary>
    /// postfix := primary ("?" | "+" | "*")?
    /// </summary>
    private Expression ParsePostfix(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var tree = ParsePrimary(tokenizer);
        var end = tokenizer.CurrentSpan.End;
        if (TryConsume(tokenizer, new Token.Symbol { Value = '?' }, out var op))
            return new Postfix(tree, op, start..end);
        if (TryConsume(tokenizer, new Token.Symbol { Value = '+' }, out op))
            return new Postfix(tree, op, start..end);
        if (TryConsume(tokenizer, new Token.Symbol { Value = '*' }, out op))
            return new Postfix(tree, op, start..end);
        return tree;
    }

    /// <summary>
    /// primary := ID | TERMINAL | STRING | "(" expression ")"
    /// </summary>
    private Expression ParsePrimary(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        switch (tokenizer.CurrentToken)
        {
            case Token.Id { Value: var id }:
            {
                var end = tokenizer.CurrentSpan.End;
                tokenizer.ScanToken();
                return new Id(id, start..end);
            }
            case Token.Terminal { Value: var t }:
            {
                var end = tokenizer.CurrentSpan.End;
                tokenizer.ScanToken();
                return new Terminal(t, start..end);
            }
            case Token.String { Value: var s }:
            {
                var end = tokenizer.CurrentSpan.End;
                tokenizer.ScanToken();
                return new String(s, start..end);
            }
            default:
                if (TryConsume(tokenizer, new Token.Symbol { Value = '(' }))
                {
                    var tree = ParseExpression(tokenizer);
                    var end = tokenizer.CurrentSpan.End;
                    Expect(tokenizer, new Token.Symbol { Value = ')' });
                    return tree with { Span = start..end };
                }
                throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        };
    }
}
