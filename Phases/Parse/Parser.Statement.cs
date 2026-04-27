using System.Collections.Immutable;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Phases.Parse;
public partial class Parser
{
    /// <summary>
    /// file := declaration*
    /// </summary>
    private File ParseFile(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var statements = ParseDeclarations(tokenizer).ToImmutableArray();
        var end = tokenizer.CurrentSpan.End;
        return new File(statements, start..end);

        IEnumerable<Declaration> ParseDeclarations(Tokenizer tokenizer)
        {
            while (tokenizer.CurrentToken is not Token.EOF)
                yield return ParseDeclaration(tokenizer);
        }
    }

    /// <summary>
    /// declaration := ID ":=" expression EOL+
    /// </summary>
    private Declaration ParseDeclaration(Tokenizer tokenizer)
    {
        var tokenSpan = tokenizer.CurrentTokenSpan;
        Expect(tokenizer, new Token.Id(), out var id);
        Expect(tokenizer, new Token.Symbol { Value = ":=" });
        var expression = ParseExpression(tokenizer);
        var end = tokenizer.CurrentSpan.End;
        Expect(tokenizer, new Token.EOL());
            while (TryConsume(tokenizer, new Token.EOL()));
        return new Declaration(new Primary(id), expression, tokenSpan.Span.Start..end);
    }
}
