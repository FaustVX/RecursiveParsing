using System.Collections.Immutable;

namespace RecursiveParsing;
public partial class Parser
{
    /// <summary>
    /// expression := conditionnal
    /// </summary>
    private ExpressionNode ParseExpression(Tokenizer tokenizer)
    => ParseConditionnal(tokenizer);

    /// <summary>
    /// conditionnal := equation "?" expression ":" conditionnal
    /// </summary>
    private ExpressionNode ParseConditionnal(Tokenizer tokenizer)
    {
        var cond = ParseEquation(tokenizer);
        if (!tokenizer.TryConsume(new Token.Symbol { Value = '?' }))
            return cond;
        var @true = ParseExpression(tokenizer);
        tokenizer.Expect(new Token.Symbol { Value = ':' });
        var @false = ParseConditionnal(tokenizer);
        return new Conditionnal(cond, @true, @false);
    }

    /// <summary>
    /// equation := relational (("==" | "!=") relational)?
    /// </summary>
    private ExpressionNode ParseEquation(Tokenizer tokenizer)
    {
        var tree = ParseRelational(tokenizer);
        if (tokenizer.TryConsume(new Token.Symbol { Value = "==" }))
            return new Equal(tree, ParseRelational(tokenizer));
        if (tokenizer.TryConsume(new Token.Symbol { Value = "!=" }))
            return new NotEqual(tree, ParseRelational(tokenizer));
        return tree;
    }

    /// <summary>
    /// relational := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?
    /// </summary>
    private ExpressionNode ParseRelational(Tokenizer tokenizer)
    {
        var tree = ParseAdditive(tokenizer);
        if (tokenizer.TryConsume(new Token.Symbol { Value = '<' }))
            return new LessThan(tree, ParseAdditive(tokenizer));
        if (tokenizer.TryConsume(new Token.Symbol { Value = "<=" }))
            return new LessThanOrEqual(tree, ParseAdditive(tokenizer));
        if (tokenizer.TryConsume(new Token.Symbol { Value = '>' }))
            return new GreaterThan(tree, ParseAdditive(tokenizer));
        if (tokenizer.TryConsume(new Token.Symbol { Value = ">=" }))
            return new GreaterThanOrEqual(tree, ParseAdditive(tokenizer));
        return tree;
    }

    /// <summary>
    /// additive := term (("+" | "-") term)*
    /// </summary>
    private ExpressionNode ParseAdditive(Tokenizer tokenizer)
    {
        var tree = ParseTerm(tokenizer);
        while (true)
            if (tokenizer.TryConsume(new Token.Symbol { Value = '+' }))
                tree = new Add(tree, ParseTerm(tokenizer));
            else if (tokenizer.TryConsume(new Token.Symbol { Value = '-' }))
                tree = new Substract(tree, ParseTerm(tokenizer));
            else
                return tree;
    }

    /// <summary>
    /// term := unary (("*" | "/") unary)*
    /// </summary>
    private ExpressionNode ParseTerm(Tokenizer tokenizer)
    {
        var tree = ParseUnary(tokenizer);
        while (true)
            if (tokenizer.TryConsume(new Token.Symbol { Value = '*' }))
                tree = new Multiply(tree, ParseUnary(tokenizer));
            else if (tokenizer.TryConsume(new Token.Symbol { Value = '/' }))
                tree = new Divide(tree, ParseUnary(tokenizer));
            else
                return tree;
    }

    /// <summary>
    /// unary := ("+" | "-") unary | exponentiation
    /// </summary>
    private ExpressionNode ParseUnary(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        if (tokenizer.TryConsume(new Token.Symbol { Value = '-' }))
        {
            var node = ParseUnary(tokenizer);
            return new Negate(node, start..node.Span.End);
        }
        if (tokenizer.TryConsume(new Token.Symbol { Value = '+' }))
        {
            var node = ParseUnary(tokenizer);
            return node with { Span = start..node.Span.End };
        }
        return ParseExponentiation(tokenizer);
    }

    /// <summary>
    /// exponentiation := postfix ("^" exponentiation)?
    /// </summary>
    private ExpressionNode ParseExponentiation(Tokenizer tokenizer)
    {
        var tree = ParsePostfix(tokenizer);
        if (tokenizer.TryConsume(new Token.Symbol { Value = '^' }))
            return new Power(tree, ParseExponentiation(tokenizer));
        return tree;
    }

    /// <summary>
    /// postfix := primary ("!" | "(" args ")")*
    /// </summary>
    private ExpressionNode ParsePostfix(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        var tree = ParsePrimary(tokenizer);
        var end = tokenizer.NextSpan.End;
        while (true)
            if (tokenizer.TryConsume(new Token.Symbol { Value = '!' }))
                tree = new Factorial(tree, start..end);
            else if (tokenizer.TryConsume(new Token.Symbol { Value = '(' }))
            {
                var args = ParseArgs(tokenizer).ToImmutableArray();
                end = tokenizer.NextSpan.End;
                tokenizer.Expect(new Token.Symbol { Value = ')' });
                tree = new Invocation(tree, args, start..end);
            }
            else
                return tree;
    }

    /// <summary>
    /// primary := ID | NUMBER | STRING | "(" expression ")"
    /// </summary>
    private ExpressionNode ParsePrimary(Tokenizer tokenizer)
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
            case Token.Int { Value: var i }:
            {
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return new Number(i, start..end);
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

    /// <summary>
    /// args := ( expression ( "," expression)* )?
    /// </summary>
    private IEnumerable<ExpressionNode> ParseArgs(Tokenizer tokenizer)
    {
        if (tokenizer.NextToken is Token.Symbol { Value: ')'})
            yield break;
        var arg = ParseExpression(tokenizer);
        yield return arg;
        while (true)
            if (tokenizer.TryConsume(new Token.Symbol { Value = ',' }))
                yield return ParseExpression(tokenizer);
            else
                yield break;
    }
}
