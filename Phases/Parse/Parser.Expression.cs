using System.Collections.Immutable;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Phases.Parse;
public partial class Parser
{
    /// <summary>
    /// expression: expression := choice
    /// </summary>
    private Expression ParseExpression(Tokenizer tokenizer)
    => ParseChoice(tokenizer);

    /// <summary>
    /// choice: expression := sequence ("|" sequence)*
    /// </summary>
    private Expression ParseChoice(Tokenizer tokenizer)
    {
        var choices = ParseSequences(tokenizer).ToImmutableArray();
        if (choices is [])
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        if (choices is [var expr])
            return expr;
        return new Choice(choices);

        IEnumerable<Expression> ParseSequences(Tokenizer tokenizer)
        {
            yield return ParseSequence(tokenizer);
            while (TryConsume(tokenizer, new Token.Symbol { Value = "|" }))
                yield return ParseSequence(tokenizer);
        }
    }

    /// <summary>
    /// sequence: expression := postfix+
    /// </summary>
    private Expression ParseSequence(Tokenizer tokenizer)
    {
        var sequences = ParseMultiple(ParsePostfix, tokenizer, ts => ts is { Token: not (Token.Id or Token.Terminal or Token.String or Token.Symbol { Value: "(" }) });
        if (sequences is [])
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        if (sequences is [var expr])
            return expr;
        return new Sequence(sequences);
    }

    /// <summary>
    /// postfix: expression := primary ("?" | "+" | "*")?
    /// </summary>
    private Expression ParsePostfix(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var tree = ParsePrimary(tokenizer);
        if (TryConsume(tokenizer, new Token.Symbol { Value = "?" }, out var op))
            return new Postfix(tree, op, start..op.Span.End);
        if (TryConsume(tokenizer, new Token.Symbol { Value = "+" }, out op))
            return new Postfix(tree, op, start..op.Span.End);
        if (TryConsume(tokenizer, new Token.Symbol { Value = "*" }, out op))
            return new Postfix(tree, op, start..op.Span.End);
        return tree;
    }

    /// <summary>
    /// primary: expression := ID | TERMINAL | STRING | "(" expression ")"
    /// </summary>
    private Expression ParsePrimary(Tokenizer tokenizer)
    {
        if (TryConsume(tokenizer, new Token.Id(), out var id))
            return new Primary(id);
        if (TryConsume(tokenizer, new Token.Terminal(), out var t))
            return new Primary(t);
        if (TryConsume(tokenizer, new Token.String(), out var s))
            return new Primary(s);
        if (TryConsume(tokenizer, new Token.Symbol { Value = "(" }, out var po))
        {
            var tree = ParseExpression(tokenizer);
            Expect(tokenizer, new Token.Symbol { Value = ")" }, out var pc);
            return tree with { Span = po.Span.Start..pc.Span.End };
        }
        throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
    }
}
