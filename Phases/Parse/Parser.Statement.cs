using System.Collections.Immutable;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Phases.Parse;
public partial class Parser
{
    /// <summary>
    /// file := node+ EOL+ declaration+
    /// </summary>
    private File ParseFile(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var nodes = ParseMultiple(ParseNode, tokenizer, ts => ts is { Token: Token.EOL });
        ParseMultiple(new Token.EOL(), tokenizer);
        var statements = ParseMultiple(ParseDeclaration, tokenizer, ts => ts is { Token: Token.EOF });
        var end = tokenizer.PreviousSpan.End;
        return new File(nodes, statements, start..end);
    }

    /// <summary>
    /// node := ID call? ":" ID call? EOL
    /// </summary>
    private Node ParseNode(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Expect(tokenizer, new Token.Id(), out var id);
        var @params = TryCall(tokenizer);
        TokenSpan node = default;
        if (TryConsume(tokenizer, new Token.Symbol { Value = ":" }))
            Expect(tokenizer, new Token.Id(), out node);
        var args = TryCall(tokenizer);
        Expect(tokenizer, new Token.EOL());
        var end = tokenizer.PreviousSpan.End;
        return new Node(new Primary(id), @params, new Primary(node), args, start..end);

        ImmutableArray<Expression> TryCall(Tokenizer tokenizer)
        => tokenizer.CurrentToken is Token.Symbol { Value : "(" }
            ? ParseCall(tokenizer)
            : [];
    }

    /// <summary>
    /// declaration := ID (":" ID)? ":=" expression EOL
    /// </summary>
    private Declaration ParseDeclaration(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Expect(tokenizer, new Token.Id(), out var id);
        TokenSpan node = default;
        if (TryConsume(tokenizer, new Token.Symbol { Value = ":" }))
            Expect(tokenizer, new Token.Id(), out node);
        Expect(tokenizer, new Token.Symbol { Value = ":=" });
        var expression = ParseExpression(tokenizer);
        Expect(tokenizer, new Token.EOL());
        var end = tokenizer.PreviousSpan.End;
        return new Declaration(new Primary(id), (node == default) ? default : new Primary(node), expression, start..end);
    }

    /// <summary>
    /// call := "(" args? ")"
    /// </summary>
    private ImmutableArray<Expression> ParseCall(Tokenizer tokenizer)
    {
        Expect(tokenizer, new Token.Symbol { Value = "(" });
        if (TryConsume(tokenizer, new Token.Symbol { Value = ")" }))
            return [];
        var args = ParseArgs(tokenizer).ToImmutableArray();
        Expect(tokenizer, new Token.Symbol { Value = ")" });
        return args;
    }

    /// <summary>
    /// args := expression ( "," expression)*
    /// </summary>
    private IEnumerable<Expression> ParseArgs(Tokenizer tokenizer)
    {
        var arg = ParseExpression(tokenizer);
        yield return arg;
        while (TryConsume(tokenizer, new Token.Symbol { Value = "," }))
            yield return ParseExpression(tokenizer);
    }
}
