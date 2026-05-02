
using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
// using EBNFParser.Phases.Tokenize;

namespace RecursiveParsing;

public partial class Parser(string input)
{
    public Tokenizer Tokenizer { get; } = new(input);

    /// <summary>
    /// <c>statement := blockstatement</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Primary (blockstatement);
    /// var end = tokenizer.CurrentSpan.End;
    /// return new statement(statements, start..end);
    /// </code>
    /// </remarks>
    private partial statement Parse_statement(Tokenizer tokenizer);

    /// <summary>
    /// <c>blockstatement := "{" statement* "}" | expressionstatement</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Choice ("{" statement* "}" | expressionstatement):
    ///     Parse_Sequence ("{" statement* "}"):
    ///         Parse_Primary ("{");
    ///         Parse_Postfix (statement*):
    ///             Parse_Primary (statement);
    ///         Parse_Primary ("}");
    ///     Parse_Primary (expressionstatement);
    /// var end = tokenizer.CurrentSpan.End;
    /// return new blockstatement(statements, start..end);
    /// </code>
    /// </remarks>
    private partial blockstatement Parse_blockstatement(Tokenizer tokenizer);

    /// <summary>
    /// <c>expressionstatement := expression ";"</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (expression ";"):
    ///     Parse_Primary (expression);
    ///     Parse_Primary (";");
    /// var end = tokenizer.CurrentSpan.End;
    /// return new expressionstatement(statements, start..end);
    /// </code>
    /// </remarks>
    private partial expressionstatement Parse_expressionstatement(Tokenizer tokenizer);

    /// <summary>
    /// <c>expression := conditionnal</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Primary (conditionnal);
    /// var end = tokenizer.CurrentSpan.End;
    /// return new expression(statements, start..end);
    /// </code>
    /// </remarks>
    private partial TreeNode Parse_expression(Tokenizer tokenizer);

    /// <summary>
    /// <c>conditionnal := equation "?" expression ":" conditionnal</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (equation "?" expression ":" conditionnal):
    ///     Parse_Primary (equation);
    ///     Parse_Primary ("?");
    ///     Parse_Primary (expression);
    ///     Parse_Primary (":");
    ///     Parse_Primary (conditionnal);
    /// var end = tokenizer.CurrentSpan.End;
    /// return new conditionnal(statements, start..end);
    /// </code>
    /// </remarks>
    private partial conditionnal Parse_conditionnal(Tokenizer tokenizer);

    /// <summary>
    /// <c>equation := relational (("==" | "!=") relational)?</c>
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
    /// return new equation(statements, start..end);
    /// </code>
    /// </remarks>
    private partial equation Parse_equation(Tokenizer tokenizer);

    /// <summary>
    /// <c>relational := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (additive (("<" | ">" | "<=" | ">=") additive)?):
    ///     Parse_Primary (additive);
    ///     Parse_Postfix ((("<" | ">" | "<=" | ">=") additive)?):
    ///         Parse_Sequence (("<" | ">" | "<=" | ">=") additive):
    ///             Parse_Choice ("<" | ">" | "<=" | ">="):
    ///                 Parse_Primary ("<");
    ///                 Parse_Primary (">");
    ///                 Parse_Primary ("<=");
    ///                 Parse_Primary (">=");
    ///             Parse_Primary (additive);
    /// var end = tokenizer.CurrentSpan.End;
    /// return new relational(statements, start..end);
    /// </code>
    /// </remarks>
    private partial relational Parse_relational(Tokenizer tokenizer);

    /// <summary>
    /// <c>additive := term (("+" | "-") term)*</c>
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
    /// return new additive(statements, start..end);
    /// </code>
    /// </remarks>
    private partial additive Parse_additive(Tokenizer tokenizer);

    /// <summary>
    /// <c>term := unary (("*" | "/") unary)*</c>
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
    /// return new term(statements, start..end);
    /// </code>
    /// </remarks>
    private partial term Parse_term(Tokenizer tokenizer);

    /// <summary>
    /// <c>unary := ("+" | "-") unary | exponentiation</c>
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
    /// return new unary(statements, start..end);
    /// </code>
    /// </remarks>
    private partial unary Parse_unary(Tokenizer tokenizer);

    /// <summary>
    /// <c>exponentiation := postfix ("^" exponentiation)?</c>
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
    /// return new exponentiation(statements, start..end);
    /// </code>
    /// </remarks>
    private partial exponentiation Parse_exponentiation(Tokenizer tokenizer);

    /// <summary>
    /// <c>postfix := primary ("!" | "(" args ")")*</c>
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
    /// return new postfix(statements, start..end);
    /// </code>
    /// </remarks>
    private partial postfix Parse_postfix(Tokenizer tokenizer);

    /// <summary>
    /// <c>primary := ID | NUMBER | STRING | "(" expression ")"</c>
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
    /// return new primary(statements, start..end);
    /// </code>
    /// </remarks>
    private partial TreeNode Parse_primary(Tokenizer tokenizer);

    /// <summary>
    /// <c>args := (expression ("," expression)*)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Postfix ((expression ("," expression)*)?):
    ///     Parse_Sequence (expression ("," expression)*):
    ///         Parse_Primary (expression);
    ///         Parse_Postfix (("," expression)*):
    ///             Parse_Sequence ("," expression):
    ///                 Parse_Primary (",");
    ///                 Parse_Primary (expression);
    /// var end = tokenizer.CurrentSpan.End;
    /// return new args(statements, start..end);
    /// </code>
    /// </remarks>
    private partial args Parse_args(Tokenizer tokenizer);

    protected static class Helper
    {
        public static Func<Tokenizer, ImmutableArray<TAny>> ParseAny<TAny>(Func<Tokenizer, TAny> parser, Func<TokenSpan, bool> endOfParse)
        where TAny : TreeNode
        => t => ParseAny(parser, t, endOfParse);
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

        public static Func<Tokenizer, ImmutableArray<Token>> ParseAny(Token token)
        => t => ParseAny(token, t);
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

        public static Func<Tokenizer, ImmutableArray<TMultiple>> ParseMultiple<TMultiple>(Func<Tokenizer, TMultiple> parser, Func<TokenSpan, bool> endOfParse)
        where TMultiple : TreeNode
        => t => ParseMultiple(parser, t, endOfParse);
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

        public static Func<Tokenizer, ImmutableArray<Token>> ParseMultiple(Token token)
        => t => ParseMultiple(token, t);
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
            tokenSpan = tokenizer.CurrentTokenSpan;
            if (tokenizer.CurrentToken != token)
                throw new ParserExpectedException(tokenizer.CurrentTokenSpan, token);
            tokenizer.ScanToken();
        }

        public static bool TryConsume(Tokenizer tokenizer, Token token)
        => TryConsume(tokenizer, token, out _);

        public static bool TryConsume(Tokenizer tokenizer, Token token, out TokenSpan tokenSpan)
        {
            tokenSpan = tokenizer.CurrentTokenSpan;
            if (tokenizer.CurrentToken != token)
                return false;
            tokenizer.ScanToken();
            return true;
        }
    }
}

[Serializable]
public abstract class ParserException(TokenSpan tokenSpan) : Exception
{
    public TokenSpan TokenSpan { get; } = tokenSpan;
}

[Serializable]
public class ParserUnexpectedException(TokenSpan tokenSpan) : ParserException(tokenSpan)
{
    public override string ToString()
    => $"Unexpected token ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
}

[Serializable]
public class ParserExpectedException(TokenSpan tokenSpan, Token expected) : ParserException(tokenSpan)
{
    public Token Expected { get; } = expected;

    public override string ToString()
    => $"Expected token {Expected} but got ({TokenSpan.Token}) at pos: {TokenSpan.Span}\n" + base.ToString();
}
