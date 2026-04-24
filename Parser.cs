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

[Serializable]
public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
{
    public Token Expected { get; } = expected;

    public override string ToString()
    {
        Console.Error.WriteLine($"Expected token {Expected}");
        return base.ToString();
    }
}

public partial class Parser
{
    /// <summary>
    /// • statement                 := expression-statement ";"
    /// <br/>
    /// • expression-statement      := expression ";"
    /// </summary>
    public StatementNode ParseStatement(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = Parse(tokenizer);
        tokenizer.Expect(new Token.EOL());
        return tree;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        StatementNode Parse(Tokenizer tokenizer)
        {
            try
            {
                return ParseStatement(tokenizer);
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
    /// • expression                := conditionnal
    /// <br/>
    /// • conditionnal              := equation "?" expression ":" conditionnal
    /// <br/>
    /// • equation                  := relational (("==" | "!=") relational)?
    /// <br/>
    /// • relational                := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?
    /// <br/>
    /// • additive                  := term (("+" | "-") term)*
    /// <br/>
    /// • term                      := unary (("*" | "/") unary)*
    /// <br/>
    /// • unary                     := ("+" | "-") unary | exponentiation
    /// <br/>
    /// • exponentiation            := postfix ("^" exponentiation)?
    /// <br/>
    /// • postfix                   := primary ("!" | "(" args ")")*
    /// <br/>
    /// • primary                   := ID | NUMBER | "(" expression ")"
    /// <br/>
    /// • args                      := ( expression ( "," expression)* )?
    /// </summary>
    public ExpressionNode ParseExpression(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.ScanToken();
        var tree = Parse(tokenizer);
        tokenizer.Expect(new Token.EOL());
        return tree;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ExpressionNode Parse(Tokenizer tokenizer)
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

}
