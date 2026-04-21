namespace RecursiveParsing;

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
    /// • postfix           := primary "!"*
    /// <br/>
    /// • primary           := ID | NUMBER | "(" expression ")"
    /// </summary>
    public TreeNode? Parse(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = ParseExpression(tokenizer);
        if (tokenizer.NextToken is not Token.EOL)
            return null;
        return tree;
    }

    /// <summary>
    /// expression := relational (("==" | "!=") relational)?
    /// </summary>
    private TreeNode? ParseExpression(Tokenizer tokenizer)
    {
        var tree = ParseRelational(tokenizer);
        if (tree is null)
            return null;
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: "==" }:
            {
                tokenizer.ScanToken();
                var right = ParseRelational(tokenizer);
                if (right is null)
                    return null;
                return new Equal(tree, right);
            }
            case Token.Symbol { Value: "!=" }:
            {
                tokenizer.ScanToken();
                var right = ParseRelational(tokenizer);
                if (right is null)
                    return null;
                return new NotEqual(tree, right);
            }
            default:
                return tree;
        }
    }

    /// <summary>
    /// relational := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?
    /// </summary>
    private TreeNode? ParseRelational(Tokenizer tokenizer)
    {
        var tree = ParseAdditive(tokenizer);
        if (tree is null)
            return null;
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '<' }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                if (right is null)
                    return null;
                return new LessThan(tree, right);
            }
            case Token.Symbol { Value: "<=" }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                if (right is null)
                    return null;
                return new LessThanOrEqual(tree, right);
            }
            case Token.Symbol { Value: '>' }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                if (right is null)
                    return null;
                return new GreaterThan(tree, right);
            }
            case Token.Symbol { Value: ">=" }:
            {
                tokenizer.ScanToken();
                var right = ParseAdditive(tokenizer);
                if (right is null)
                    return null;
                return new GreaterThanOrEqual(tree, right);
            }
            default:
                return tree;
        }
    }

    /// <summary>
    /// additive := term (("+" | "-") term)*
    /// </summary>
    private TreeNode? ParseAdditive(Tokenizer tokenizer)
    {
        var tree = ParseTerm(tokenizer);
        if (tree is null)
            return null;
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '+' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseTerm(tokenizer);
                    if (right is null)
                        return null;
                    tree = new Add(tree, right);
                    break;
                }
                case Token.Symbol { Value: '-' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseTerm(tokenizer);
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
    private TreeNode? ParseTerm(Tokenizer tokenizer)
    {
        var tree = ParseUnary(tokenizer);
        if (tree is null)
            return null;
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '*' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseUnary(tokenizer);
                    if (right is null)
                        return null;
                    tree = new Multiply(tree, right);
                    break;
                }
                case Token.Symbol { Value: '/' }:
                {
                    tokenizer.ScanToken();
                    var right = ParseUnary(tokenizer);
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
    /// unary := ("+" | "-") unary | exponentiation
    /// </summary>
    private TreeNode? ParseUnary(Tokenizer tokenizer)
    {
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '-' }:
            {
                tokenizer.ScanToken();
                var node = ParseUnary(tokenizer);
                if (node is null)
                    return null;
                return new Negate(node);
            }
            case Token.Symbol { Value: '+' }:
            {
                tokenizer.ScanToken();
                var node = ParseUnary(tokenizer);
                    return node;
            }
            default:
                return ParseExponentiation(tokenizer);
        };
    }

    /// <summary>
    /// exponentiation := postfix ("^" exponentiation)?
    /// </summary>
    private TreeNode? ParseExponentiation(Tokenizer tokenizer)
    {
        var tree = ParsePostfix(tokenizer);
        if (tree is null)
            return null;
        switch (tokenizer.NextToken)
        {
            case Token.Symbol { Value: '^' }:
            {
                tokenizer.ScanToken();
                var right = ParseExponentiation(tokenizer);
                if (right is null)
                    return null;
                return new Power(tree, right);
            }
            default:
                return tree;
        }
    }

    /// <summary>
    /// postfix := primary "!"*
    /// </summary>
    private TreeNode? ParsePostfix(Tokenizer tokenizer)
    {
        var tree = ParsePrimary(tokenizer);
        if (tree is null)
            return null;
        while (true)
            switch (tokenizer.NextToken)
            {
                case Token.Symbol { Value: '!' }:
                    tokenizer.ScanToken();
                    tree = new Factorial(tree);
                    break;
                default:
                    return tree;
            }
    }

    /// <summary>
    /// primary := ID | NUMBER | "(" expression ")"
    /// </summary>
    private TreeNode? ParsePrimary(Tokenizer tokenizer)
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
                var tree = ParseExpression(tokenizer);
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
