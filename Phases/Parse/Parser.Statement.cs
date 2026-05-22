using System.Collections.Immutable;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Phases.Parse;
public partial class Parser
{
    /// <summary>
    /// file: file := node+ EOL+ declaration+
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
    /// node: node := postfix call? ":" ID call? EOL
    /// </summary>
    private Node ParseNode(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var id = ParsePostfix(tokenizer) is Postfix post ? post : throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        var @params = TryCall(tokenizer).Cast<Sequence>().ToImmutableArray();
        TokenSpan node = default;
        if (TryConsume(tokenizer, new Token.Symbol { Value = ":" }))
            Expect(tokenizer, new Token.Id(), out node);
        var args = TryCall(tokenizer).Cast<Primary>().ToImmutableArray();
        Expect(tokenizer, new Token.EOL());
        var end = tokenizer.PreviousSpan.End;
        return new Node(id, @params, new Primary(node), args, start..end);

        ImmutableArray<Expression> TryCall(Tokenizer tokenizer)
        => tokenizer.CurrentToken is Token.Symbol { Value : "(" }
            ? ParseCall(tokenizer)
            : [];
    }

    /// <summary>
    /// declaration: declaration := ID ":" postfix ":=" expression EOL
    /// </summary>
    private Declaration ParseDeclaration(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Expect(tokenizer, new Token.Id(), out var id);
        Expect(tokenizer, new Token.Symbol { Value = ":" });
        var node = ParsePostfix(tokenizer);
        Expect(tokenizer, new Token.Symbol { Value = ":=" });
        var expression = ParseExpression(tokenizer);
        Expect(tokenizer, new Token.EOL());
        var end = tokenizer.PreviousSpan.End;
        return new Declaration(new Primary(id), node, expression, start..end);
    }

    /// <summary>
    /// call: expression* := "(" args? ")"
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
    /// args: expression* := expression ("," expression)*
    /// </summary>
    private IEnumerable<Expression> ParseArgs(Tokenizer tokenizer)
    {
        var arg = ParseExpression(tokenizer);
        yield return arg;
        while (TryConsume(tokenizer, new Token.Symbol { Value = "," }))
            yield return ParseExpression(tokenizer);
    }
}
