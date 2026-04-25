#pragma warning disable CA1859 // Use concrete types when possible for improved performance
using System.Collections.Immutable;

namespace RecursiveParsing;
public partial class Parser
{
    /// <summary>
    /// file := declaration*
    /// </summary>
    private File ParseFile(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        var statements = ParseDeclarations(tokenizer).ToImmutableArray();
        var end = tokenizer.NextSpan.End;
        return new File(statements, start..end);

        IEnumerable<Declaration> ParseDeclarations(Tokenizer tokenizer)
        {
            while (tokenizer.NextToken is not Token.EOF)
                yield return ParseDeclaration(tokenizer);
        }
    }

    /// <summary>
    /// declaration := ID ":=" expression EOL+
    /// </summary>
    private Declaration ParseDeclaration(Tokenizer tokenizer)
    {
        var tokenSpan = tokenizer.NextTokenSpan;
        var primary = ParsePrimary(tokenizer);
        if (primary is not Id id)
            throw new ParserException(tokenSpan);
        tokenizer.Expect(new Token.Symbol { Value = ":=" });
        var expression = ParseExpression(tokenizer);
        var end = tokenizer.NextSpan.End;
        tokenizer.Expect(new Token.EOL());
            while (tokenizer.TryConsume(new Token.EOL()));
        return new Declaration(id, expression, tokenSpan.Span.Start..end);
    }
}
