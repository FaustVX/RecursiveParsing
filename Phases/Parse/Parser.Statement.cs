#pragma warning disable CA1859 // Use concrete types when possible for improved performance
using System.Collections.Immutable;
using RecursiveParsing.Phases.Tokenize;

namespace RecursiveParsing.Phases.Parse;
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
        Expect(tokenizer, new Token.Id(), out var id);
        Expect(tokenizer, new Token.Symbol { Value = ":=" });
        var expression = ParseExpression(tokenizer);
        var end = tokenizer.NextSpan.End;
        Expect(tokenizer, new Token.EOL());
            while (TryConsume(tokenizer, new Token.EOL()));
        return new Declaration(new Id(id.Token.TokenString(), id.Span), expression, tokenSpan.Span.Start..end);
    }
}
