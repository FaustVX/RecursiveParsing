#pragma warning disable CA1859 // Use concrete types when possible for improved performance
using System.Collections.Immutable;

namespace RecursiveParsing;
public partial class Parser
{
    /// <summary>
    /// statement := block-statement
    /// </summary>
    private StatementNode ParseStatement(Tokenizer tokenizer)
    => ParseBlockStatement(tokenizer);

    /// <summary>
    /// block-statement := ("{" (statement)* "}") | expression-statement
    /// </summary>
    private StatementNode ParseBlockStatement(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        if (!tokenizer.TryConsume(new Token.Symbol { Value = '{' }))
            return ParseExpressionStatement(tokenizer);
        var statements = ParseStatements(tokenizer).ToImmutableArray();
        var end = tokenizer.NextSpan.End;
        tokenizer.Expect(new Token.Symbol { Value = '}' });
        return new BlockStatement(statements, start..end);

        IEnumerable<StatementNode> ParseStatements(Tokenizer tokenizer)
        {
            if (tokenizer.NextToken is Token.Symbol { Value: '}' })
                yield break;
            var arg = ParseExpressionStatement(tokenizer);
            yield return arg;
            while (tokenizer.NextToken is not Token.Symbol { Value: '}' })
                yield return ParseExpressionStatement(tokenizer);
        }
    }

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
