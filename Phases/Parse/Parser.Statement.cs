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
        var statements = ParseAny(ParseDeclaration, tokenizer, ts => ts is { Token: Token.EOF });
        var end = tokenizer.PreviousSpan.End;
        return new File(statements, start..end);
    }

    /// <summary>
    /// declaration := ID ":=" expression EOL+
    /// </summary>
    private Declaration ParseDeclaration(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Expect(tokenizer, new Token.Id(), out var id);
        Expect(tokenizer, new Token.Symbol { Value = ":=" });
        var expression = ParseExpression(tokenizer);
        _ = ParseMultiple(new Token.EOL(), tokenizer);
        var end = tokenizer.PreviousSpan.End;
        return new Declaration(new Primary(id), expression, start..end);
    }
}
