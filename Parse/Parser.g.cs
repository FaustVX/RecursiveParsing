#nullable enable
using System.Collections.Immutable;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Parse;

public partial class Parser(string input)
{
    public Tokenizer Tokenizer { get; } = new(input);

    /// <summary>
    /// <c>statement : statement := block-statement | expression-statement</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Choice (block-statement | expression-statement):
    ///     Parse_Primary (block-statement);
    ///     Parse_Primary (expression-statement);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Statement Parse_Statement(Tokenizer tokenizer);

    /// <summary>
    /// <c>block-statement : block-statement := "{" statement* "}"</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence ("{" statement* "}"):
    ///     Parse_Primary ("{");
    ///     Parse_Postfix (statement*):
    ///         Parse_Primary (statement);
    ///     Parse_Primary ("}");
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial BlockStatement Parse_BlockStatement(Tokenizer tokenizer);

    /// <summary>
    /// <c>expression-statement : expression-statement := expression ";"</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (expression ";"):
    ///     Parse_Primary (expression);
    ///     Parse_Primary (";");
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial ExpressionStatement Parse_ExpressionStatement(Tokenizer tokenizer);

    /// <summary>
    /// <c>expression : expression := conditionnal</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Primary (conditionnal);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Expression(Tokenizer tokenizer);

    /// <summary>
    /// <c>conditionnal : expression := equation ("?" expression ":" conditionnal)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (equation ("?" expression ":" conditionnal)?):
    ///     Parse_Primary (equation);
    ///     Parse_Postfix (("?" expression ":" conditionnal)?):
    ///         Parse_Sequence ("?" expression ":" conditionnal):
    ///             Parse_Primary ("?");
    ///             Parse_Primary (expression);
    ///             Parse_Primary (":");
    ///             Parse_Primary (conditionnal);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Conditionnal(Tokenizer tokenizer);

    /// <summary>
    /// <c>equation : expression := relational (("==" | "!=") relational)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (relational (("==" | "!=") relational)?):
    ///     Parse_Primary (relational);
    ///     Parse_Postfix ((("==" | "!=") relational)?):
    ///         Parse_Sequence (("==" | "!=") relational):
    ///             Parse_Choice ("==" | "!="):
    ///                 Parse_Primary ("==");
    ///                 Parse_Primary ("!=");
    ///             Parse_Primary (relational);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Equation(Tokenizer tokenizer);

    /// <summary>
    /// <c>relational : expression := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?):
    ///     Parse_Primary (additive);
    ///     Parse_Postfix ((("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?):
    ///         Parse_Sequence (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive):
    ///             Parse_Choice ("&lt;" | "&gt;" | "&lt;=" | "&gt;="):
    ///                 Parse_Primary ("&lt;");
    ///                 Parse_Primary ("&gt;");
    ///                 Parse_Primary ("&lt;=");
    ///                 Parse_Primary ("&gt;=");
    ///             Parse_Primary (additive);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Relational(Tokenizer tokenizer);

    /// <summary>
    /// <c>additive : expression := term (("+" | "-") term)*</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (term (("+" | "-") term)*):
    ///     Parse_Primary (term);
    ///     Parse_Postfix ((("+" | "-") term)*):
    ///         Parse_Sequence (("+" | "-") term):
    ///             Parse_Choice ("+" | "-"):
    ///                 Parse_Primary ("+");
    ///                 Parse_Primary ("-");
    ///             Parse_Primary (term);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Additive(Tokenizer tokenizer);

    /// <summary>
    /// <c>term : expression := unary (("*" | "/") unary)*</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (unary (("*" | "/") unary)*):
    ///     Parse_Primary (unary);
    ///     Parse_Postfix ((("*" | "/") unary)*):
    ///         Parse_Sequence (("*" | "/") unary):
    ///             Parse_Choice ("*" | "/"):
    ///                 Parse_Primary ("*");
    ///                 Parse_Primary ("/");
    ///             Parse_Primary (unary);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Term(Tokenizer tokenizer);

    /// <summary>
    /// <c>unary : expression := ("+" | "-") unary | exponentiation</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Choice (("+" | "-") unary | exponentiation):
    ///     Parse_Sequence (("+" | "-") unary):
    ///         Parse_Choice ("+" | "-"):
    ///             Parse_Primary ("+");
    ///             Parse_Primary ("-");
    ///         Parse_Primary (unary);
    ///     Parse_Primary (exponentiation);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Unary(Tokenizer tokenizer);

    /// <summary>
    /// <c>exponentiation : expression := postfix ("^" exponentiation)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (postfix ("^" exponentiation)?):
    ///     Parse_Primary (postfix);
    ///     Parse_Postfix (("^" exponentiation)?):
    ///         Parse_Sequence ("^" exponentiation):
    ///             Parse_Primary ("^");
    ///             Parse_Primary (exponentiation);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Exponentiation(Tokenizer tokenizer);

    /// <summary>
    /// <c>postfix : expression := primary ("!" | "(" args ")")*</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (primary ("!" | "(" args ")")*):
    ///     Parse_Primary (primary);
    ///     Parse_Postfix (("!" | "(" args ")")*):
    ///         Parse_Choice ("!" | "(" args ")"):
    ///             Parse_Primary ("!");
    ///             Parse_Sequence ("(" args ")"):
    ///                 Parse_Primary ("(");
    ///                 Parse_Primary (args);
    ///                 Parse_Primary (")");
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Postfix(Tokenizer tokenizer);

    /// <summary>
    /// <c>primary : expression := ID | NUMBER | STRING | "(" expression ")"</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Choice (ID | NUMBER | STRING | "(" expression ")"):
    ///     Parse_Primary (ID);
    ///     Parse_Primary (NUMBER);
    ///     Parse_Primary (STRING);
    ///     Parse_Sequence ("(" expression ")"):
    ///         Parse_Primary ("(");
    ///         Parse_Primary (expression);
    ///         Parse_Primary (")");
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial Expression Parse_Primary(Tokenizer tokenizer);

    /// <summary>
    /// <c>args : expression* := (expression ("," expression)*)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Postfix (expression*):
    /// Parse_Postfix ((expression ("," expression)*)?):
    ///     Parse_Sequence (expression ("," expression)*):
    ///         Parse_Primary (expression);
    ///         Parse_Postfix (("," expression)*):
    ///             Parse_Sequence ("," expression):
    ///                 Parse_Primary (",");
    ///                 Parse_Primary (expression);
    /// var end = tokenizer.CurrentSpan.End;
    /// </code>
    /// </remarks>
    private partial System.Collections.Immutable.ImmutableArray<Expression> Parse_Args(Tokenizer tokenizer);

    protected static class Helper
    {
        public static ImmutableArray<TAny> ParseAny<TAny>(Func<Tokenizer, TAny> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
        where TAny : TreeNode
        {
            return [.. ParseNodes(parser, tokenizer, endOfParse)];

            static IEnumerable<TAny> ParseNodes(Func<Tokenizer, TAny> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
            {
                while (!endOfParse(tokenizer.CurrentTokenSpan))
                    yield return parser(tokenizer);
            }
        }

        public static ImmutableArray<Token> ParseAny(Token token, Tokenizer tokenizer)
        {
            return [.. ParseTokens(token, tokenizer)];

            static IEnumerable<Token> ParseTokens(Token token, Tokenizer tokenizer)
            {
                Expect(tokenizer, token, out var ts);
                yield return ts.Token;
                while (TryConsume(tokenizer, token, out ts))
                    yield return ts.Token;
            }
        }

        public static ImmutableArray<TMultiple> ParseMultiple<TMultiple>(Func<Tokenizer, TMultiple> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
        where TMultiple : TreeNode
        {
            return [.. ParseNodes(parser, tokenizer, endOfParse)];

            static IEnumerable<TMultiple> ParseNodes(Func<Tokenizer, TMultiple> parser, Tokenizer tokenizer, Func<TokenSpan, bool> endOfParse)
            {
                while (!endOfParse(tokenizer.CurrentTokenSpan))
                    yield return parser(tokenizer);
            }
        }

        public static ImmutableArray<Token> ParseMultiple(Token token, Tokenizer tokenizer)
        {
            return [.. ParseTokens(token, tokenizer)];

            static IEnumerable<Token> ParseTokens(Token token, Tokenizer tokenizer)
            {
                Expect(tokenizer, token, out var ts);
                yield return ts.Token;
                while (TryConsume(tokenizer, token, out ts))
                    yield return ts.Token;
            }
        }

        public static void Expect(Tokenizer tokenizer, Token token)
        => Expect(tokenizer, token, out _);

        public static void Expect(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
        {
            if (!TryConsume(tokenizer, token, out tokenSpan))
                throw new ParserExpectedException(tokenizer.CurrentTokenSpan, token);
        }

        public static bool TryConsume(Tokenizer tokenizer, Token token)
        => TryConsume(tokenizer, token, out _);

        public static bool TryConsume(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
        {
            if (tokenizer.CurrentToken != token)
            {
                tokenSpan = default;
                return false;
            }
            tokenSpan = tokenizer.CurrentTokenSpan;
            tokenizer.ScanToken();
            return true;
        }
    }
}

[Serializable]
public abstract class ParserException(TokenSpan tokenSpan) : EBNFException
{
    public TokenSpan TokenSpan { get; } = tokenSpan;
    public override Range Range => TokenSpan.Span;
}

[Serializable]
public class ParserUnexpectedException(TokenSpan tokenSpan) : ParserException(tokenSpan)
{
    public override string ErrorCode => "EB_0003";
    public override string SubCategory => "Unexpected Parser";
    public override string Message
    => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}";
}

[Serializable]
public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
{
    public Token Expected { get; } = expected;
    public override string ErrorCode => "EB_0004";
    public override string SubCategory => "Expected Parser";

    public override string Message
    => $"Expected token {Expected} but got ({TokenSpan.Token}) at pos: {TokenSpan.Span}";
}
