#pragma warning disable CA1859 // Use concrete types when possible for improved performance
namespace RecursiveParsing;
public partial class Parser
{
    /// <summary>
    /// statement := expression-statement
    /// </summary>
    private StatementNode ParseStatement(Tokenizer tokenizer)
    => ParseExpressionStatement(tokenizer);

    /// <summary>
    /// expression-statement := expression ";"
    /// </summary>
    private StatementNode ParseExpressionStatement(Tokenizer tokenizer)
    {
        var expression = ParseExpression(tokenizer);
        var end = tokenizer.NextSpan.End;
        tokenizer.Expect(new Token.Symbol { Value = ';' });
        return new ExpressionStatement(expression, expression.Span.Start..end);
    }
}
