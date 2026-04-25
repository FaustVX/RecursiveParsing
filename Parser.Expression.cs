using System.Collections.Immutable;

namespace RecursiveParsing;
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
            throw new ParserException(default);
        if (choices is [var expr])
            return expr;
        return new Choice(choices);

        IEnumerable<Expression> ParseSequences(Tokenizer tokenizer)
        {
            yield return ParseSequence(tokenizer);
            while (tokenizer.TryConsume(new Token.Symbol { Value = '|' }))
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
            throw new ParserException(default);
        if (sequences is [var expr])
            return expr;
        return new Sequence(sequences);

        IEnumerable<Expression> ParsePostfixes(Tokenizer tokenizer)
        {
            yield return ParsePostfix(tokenizer);
            while (tokenizer.NextToken is Token.Id or Token.Terminal or Token.String or Token.Symbol { Value: '(' })
                yield return ParsePostfix(tokenizer);
        }
    }

    /// <summary>
    /// postfix := primary ("?" | "+" | "*")?
    /// </summary>
    private Expression ParsePostfix(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        var tree = ParsePrimary(tokenizer);
        var end = tokenizer.NextSpan.End;
        if (tokenizer.TryConsume(new Token.Symbol { Value = '?' }))
            return new Optional(tree, start..end);
        if (tokenizer.TryConsume(new Token.Symbol { Value = '+' }))
            return new Multiple(tree, start..end);
        if (tokenizer.TryConsume(new Token.Symbol { Value = '*' }))
            return new Any(tree, start..end);
        return tree;
    }

    /// <summary>
    /// primary := ID | TERMINAL | STRING | "(" expression ")"
    /// </summary>
    private Expression ParsePrimary(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        switch (tokenizer.NextToken)
        {
            case Token.Id { Value: var id }:
            {
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return new Id(id, start..end);
            }
            case Token.Terminal { Value: var t }:
            {
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return new Terminal(t, start..end);
            }
            case Token.String { Value: var s }:
            {
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return new String(s, start..end);
            }
            default:
                if (tokenizer.TryConsume(new Token.Symbol { Value = '(' }))
                {
                    var tree = ParseExpression(tokenizer);
                    var end = tokenizer.NextSpan.End;
                    tokenizer.Expect(new Token.Symbol { Value = ')' });
                    return tree with { Span = start..end };
                }
                throw new ParserException(tokenizer.NextTokenSpan);
        };
    }
}
