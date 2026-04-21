namespace RecursiveParsing;

public class Parser()
{
    /// <summary>
    /// • expression  := term (("+" | "-") term)*
    /// <br/>
    /// • term        := unary (("*" | "/") unary)*
    /// <br/>
    /// • unary       := ("+" | "-") unary | primary
    /// <br/>
    /// • primary     := ID | NUMBER | "(" expression ")"
    /// </summary>
    public TreeNode? Parse(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = ParseExpression(ref tokenizer);
        if (tokenizer.NextToken is not Token.EOL)
            return null;
        return tree;
    }

    /// <summary>
    /// expression := term (("+" | "-") term)*
    /// </summary>
    private TreeNode? ParseExpression(ref Tokenizer tokenizer)
    {
        var tree = ParseTerm(ref tokenizer);
        if (tree is null)
            return null;
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '+' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseTerm(ref tokenizer);
                    if (right is null)
                        return null;
                    tree = new Add(tree, right);
                    break;
                }
                case Token.Symbol { Value: '-' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseTerm(ref tokenizer);
                    if (right is null)
                        return null;
                    tree = new Substract(tree, right);
                    break;
                }
                default:
                    return tree;
            }
    }

    /// <summary>
    /// term := unary (("*" | "/") unary)*
    /// </summary>
    private TreeNode? ParseTerm(ref Tokenizer tokenizer)
    {
        var tree = ParseUnary(ref tokenizer);
        if (tree is null)
            return null;
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '*' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseUnary(ref tokenizer);
                    if (right is null)
                        return null;
                    tree = new Multiply(tree, right);
                    break;
                }
                case Token.Symbol { Value: '/' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseUnary(ref tokenizer);
                    if (right is null)
                        return null;
                    tree = new Divide(tree, right);
                    break;
                }
                // case Token.Symbol { Value: '!' }:
                //     tokenizer.ScanToken();
                //     return new Factorial(tree);
                default:
                    return tree;
            }
    }

    /// <summary>
    /// unary := ("+" | "-") unary | primary
    /// </summary>
    private TreeNode? ParseUnary(ref Tokenizer tokenizer)
    {
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '-' }:
            {
                tokenizer.ScanToken();
                var node = ParseUnary(ref tokenizer);
                if (node is null)
                    return null;
                return new Negate(node);
            }
            case Token.Symbol { Value: '+' }:
            {
                tokenizer.ScanToken();
                var node = ParseUnary(ref tokenizer);
                    return node;
            }
            default:
                return ParsePrimary(ref tokenizer);
        };
    }

    /// <summary>
    /// primary := ID | NUMBER | "(" expression ")"
    /// </summary>
    private TreeNode? ParsePrimary(ref Tokenizer tokenizer)
    {
        switch (tokenizer.NextToken)
        {
            case Token.Id { Value: var id }:
                tokenizer.ScanToken();
                return new Id(id);
            case Token.Int { Value: var i }:
                tokenizer.ScanToken();
                return new Number(i);
            case Token.Symbol { Value: '(' }:
                tokenizer.ScanToken();
                var tree = ParseExpression(ref tokenizer);
                if (tree is null)
                    return null;
                if (tokenizer.NextToken is not Token.Symbol { Value: ')' })
                    return null;
                tokenizer.ScanToken();
                return tree;
            default:
                return null;
        };
    }
}
