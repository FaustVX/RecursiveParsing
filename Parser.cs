using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace RecursiveParsing;

[Serializable]
public class ParserException(TokenSpan tokenSpan) : Exception
{
    public TokenSpan TokenSpan { get; } = tokenSpan;

    public override string ToString()
    {
        Console.Error.WriteLine($"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}");
        return base.ToString();
    }
}

public class Parser()
{
    /// <summary>
    /// • expression        := relational (("==" | "!=") relational)?
    /// <br/>
    /// • relational        := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?
    /// <br/>
    /// • additive          := term (("+" | "-") term)*
    /// <br/>
    /// • term              := unary (("*" | "/") unary)*
    /// <br/>
    /// • unary             := ("+" | "-") unary | exponentiation
    /// <br/>
    /// • exponentiation    := postfix ("^" exponentiation)?
    /// <br/>
    /// • postfix           := primary ("!" | "(" args ")")*
    /// <br/>
    /// • primary           := ID | NUMBER | "(" expression ")"
    /// <br/>
    /// • args              := ( expression ( "," expression)* )?
    /// </summary>
    public TreeNode Parse(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = Parse(tokenizer);
        if (tokenizer.NextToken is not Token.EOL)
            throw new ParserException(tokenizer.NextTokenSpan);
        return tree;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        TreeNode Parse(Tokenizer tokenizer)
        {
            try
            {
                return ParseExpression(tokenizer);
            }
            catch (ParserException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

    /// <summary>
    /// expression := relational (("==" | "!=") relational)?
    /// </summary>
    private TreeNode ParseExpression(Tokenizer tokenizer)
    {
        var tree = ParseRelational(tokenizer);
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: "==" }:
            {
                tokenizer.ScanToken();
                var right = ParseRelational(tokenizer);
                return new Equal(tree, right, 0);
            }
            case Token.Symbol { Value: "!=" }:
            {
                tokenizer.ScanToken();
                var right = ParseRelational(tokenizer);
                return new NotEqual(tree, right, 0);
            }
            default:
                return tree;
        }
    }

    /// <summary>
    /// relational := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?
    /// </summary>
    private TreeNode ParseRelational(Tokenizer tokenizer)
    {
        var tree = ParseAdditive(tokenizer);
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '<' }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                return new LessThan(tree, right, 1);
            }
            case Token.Symbol { Value: "<=" }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                return new LessThanOrEqual(tree, right, 1);
            }
            case Token.Symbol { Value: '>' }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                return new GreaterThan(tree, right, 1);
            }
            case Token.Symbol { Value: ">=" }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                return new GreaterThanOrEqual(tree, right, 1);
            }
            default:
                return tree;
        }
    }

    /// <summary>
    /// additive := term (("+" | "-") term)*
    /// </summary>
    private TreeNode ParseAdditive(Tokenizer tokenizer)
    {
        var tree = ParseTerm(tokenizer);
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '+' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseTerm(tokenizer);
                    tree = new Add(tree, right, 2);
                    break;
                }
                case Token.Symbol { Value: '-' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseTerm(tokenizer);
                    tree = new Substract(tree, right, 2);
                    break;
                }
                default:
                    return tree;
            }
    }

    /// <summary>
    /// term := unary (("*" | "/") unary)*
    /// </summary>
    private TreeNode ParseTerm(Tokenizer tokenizer)
    {
        var tree = ParseUnary(tokenizer);
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '*' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseUnary(tokenizer);
                    tree = new Multiply(tree, right, 3);
                    break;
                }
                case Token.Symbol { Value: '/' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseUnary(tokenizer);
                    tree = new Divide(tree, right, 3);
                    break;
                }
                default:
                    return tree;
            }
    }

    /// <summary>
    /// unary := ("+" | "-") unary | exponentiation
    /// </summary>
    private TreeNode ParseUnary(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '-' }:
            {
                tokenizer.ScanToken();
                var node = ParseUnary(tokenizer);
                return new Negate(node, start..node.Span.End, 4);
            }
            case Token.Symbol { Value: '+' }:
            {
                tokenizer.ScanToken();
                var node = ParseUnary(tokenizer);
                    return node with { Span = start..node.Span.End };
            }
            default:
                return ParseExponentiation(tokenizer);
        };
    }

    /// <summary>
    /// exponentiation := postfix ("^" exponentiation)?
    /// </summary>
    private TreeNode ParseExponentiation(Tokenizer tokenizer)
    {
        var tree = ParsePostfix(tokenizer);
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '^' }:
            {
                tokenizer.ScanToken();
                var right = ParseExponentiation(tokenizer);
                return new Power(tree, right, 5);
            }
            default:
                return tree;
        }
    }

    /// <summary>
    /// postfix := primary ("!" | "(" args ")")*
    /// </summary>
    private TreeNode ParsePostfix(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        var tree = ParsePrimary(tokenizer);
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '!' }:
                {
                    var end = tokenizer.NextSpan.End;
                    tokenizer.ScanToken();
                    tree = new Factorial(tree, start..end, 6);
                    break;
                }
                case Token.Symbol { Value: '(' }:
                {
                    tokenizer.ScanToken();
                    var args = ParseArgs(tokenizer).ToImmutableArray();
                    var end = tokenizer.NextSpan.End;
                    if (tokenizer.NextToken is not Token.Symbol { Value: ')' })
                        throw new ParserException(tokenizer.NextTokenSpan);
                    tokenizer.ScanToken();
                    tree = new Invocation(tree, args, start..end, 6);
                    break;
                }
                default:
                    return tree;
            }
    }

    /// <summary>
    /// primary := ID | NUMBER | "(" expression ")"
    /// </summary>
    private TreeNode ParsePrimary(Tokenizer tokenizer)
    {
        var start = tokenizer.NextSpan.Start;
        switch (tokenizer.NextToken)
        {
            case Token.Id { Value: var id }:
            {
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return new Id(id, start..end, 7);
            }
            case Token.Int { Value: var i }:
            {
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return new Number(i, start..end, 7);
            }
            case Token.Symbol { Value: '(' }:
            {
                tokenizer.ScanToken();
                var tree = ParseExpression(tokenizer);
                if (tokenizer.NextToken is not Token.Symbol { Value: ')' })
                    throw new ParserException(tokenizer.NextTokenSpan);
                var end = tokenizer.NextSpan.End;
                tokenizer.ScanToken();
                return tree with { Span = start..end };
            }
            default:
                throw new ParserException(tokenizer.NextTokenSpan);
        };
    }

    /// <summary>
    /// args := ( expression ( "," expression)* )?
    /// </summary>
    private IEnumerable<TreeNode> ParseArgs(Tokenizer tokenizer)
    {
        if (tokenizer.NextToken is Token.Symbol { Value: ')'})
            yield break;
        var arg = ParseExpression(tokenizer);
        yield return arg;
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: ',' }:
                {
                    tokenizer.ScanToken();
                    arg = ParseExpression(tokenizer);
                    yield return arg;
                    break;
                }
                default:
                    yield break;
            }
    }
}
