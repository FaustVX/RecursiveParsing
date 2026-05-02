using System.Runtime.CompilerServices;

namespace RecursiveParsing;
public partial class Parser
{
    public TreeNode ParseFile()
    {
        Tokenizer.ScanToken();
        var tree = Parse(Tokenizer);
        Helper.Expect(Tokenizer, new Token.EOF());
        return tree;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        statement Parse(Tokenizer tokenizer)
        {
            try
            {
                return Parse_statement(tokenizer);
            }
            catch (ParserException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
            catch (TokenizerException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

    /// <summary>
    /// <c>statement := blockstatement</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Primary (blockstatement);
    /// var end = tokenizer.PreviousSpan.End;
    /// return new statement(statements, start..end);
    /// </code>
    /// </remarks>
    private partial statement Parse_statement(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_blockstatement(tokenizer);
        var end = tokenizer.PreviousSpan.End;
        return new(start..end);
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new blockstatement(statements, start..end);
    /// </code>
    /// </remarks>
    private partial blockstatement Parse_blockstatement(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        if (Helper.TryConsume(tokenizer, new Token.Symbol("{")))
        {
            var s = Helper.ParseAny(Parse_statement, tokenizer, t => t is { Token: Token.Symbol { Value: "}" }});
            Helper.Expect(tokenizer, new Token.String("}"));
        }
        else
        {
            var s = Parse_expressionstatement(tokenizer);
        }
        var end = tokenizer.PreviousSpan.End;
        return new(start..end);
    }

    /// <summary>
    /// <c>expressionstatement := expression ";"</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (expression ";"):
    ///     Parse_Primary (expression);
    ///     Parse_Primary (";");
    /// var end = tokenizer.PreviousSpan.End;
    /// return new expressionstatement(statements, start..end);
    /// </code>
    /// </remarks>
    private partial expressionstatement Parse_expressionstatement(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_expression(tokenizer);
        Helper.Expect(tokenizer, new Token.Symbol(";"));
        var end = tokenizer.PreviousSpan.End;
        return new(start..end);
    }

    /// <summary>
    /// <c>expression := conditionnal</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Primary (conditionnal);
    /// var end = tokenizer.PreviousSpan.End;
    /// return new expression(statements, start..end);
    /// </code>
    /// </remarks>
    private partial TreeNode Parse_expression(Tokenizer tokenizer)
    => Parse_conditionnal(tokenizer);

    /// <summary>
    /// <c>conditionnal := equation ("?" expression ":" conditionnal)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (equation ("?" expression ":" conditionnal)?):
    ///     Parse_Primary (equation);
    ///     Parse_Primary (("?" expression ":" conditionnal)?):
    ///         Parse_Sequence ("?" expression ":" conditionnal):
    ///             Parse_Primary ("?");
    ///             Parse_Primary (expression);
    ///             Parse_Primary (":");
    ///             Parse_Primary (conditionnal);
    /// var end = tokenizer.PreviousSpan.End;
    /// return new conditionnal(statements, start..end);
    /// </code>
    /// </remarks>
    private partial conditionnal Parse_conditionnal(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s0 = Parse_equation(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("?")))
        {
            var s1 = Parse_expression(tokenizer);
            Helper.Expect(tokenizer, new Token.Symbol(":"));
            var s2 = Parse_conditionnal(tokenizer);
        }
        var end = tokenizer.PreviousSpan.End;
        return new(start..end);
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new equation(statements, start..end);
    /// </code>
    /// </remarks>
    private partial equation Parse_equation(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s0 = Parse_relational(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("=="), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("!="), out t))
        {
            var s1 = Parse_relational(tokenizer);
        }
        var end = tokenizer.PreviousSpan.End;
        return new(start..end);
    }

    /// <summary>
    /// <c>relational := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?</c>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var start = tokenizer.CurrentSpan.Start;
    /// Parse_Sequence (additive (("&lt; | "&gt; | "&lt;= | "&gt;=) additive)?):
    ///     Parse_Primary (additive);
    ///     Parse_Postfix ((("&lt; | "&gt; | "&lt;= | "&gt;=) additive)?):
    ///         Parse_Sequence (("&lt; | "&gt; | "&lt;= | "&gt;=) additive):
    ///             Parse_Choice ("&lt; | "&gt; | "&lt;= | "&gt;=):
    ///                 Parse_Primary ("&lt;);
    ///                 Parse_Primary ("&gt;);
    ///                 Parse_Primary ("&lt;=);
    ///                 Parse_Primary ("&gt;=);
    ///             Parse_Primary (additive);
    /// var end = tokenizer.PreviousSpan.End;
    /// return new relational(statements, start..end);
    /// </code>
    /// </remarks>
    private partial relational Parse_relational(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_additive(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("<"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("<="), out t) || Helper.TryConsume(tokenizer, new Token.Symbol(">"), out t) || Helper.TryConsume(tokenizer, new Token.Symbol(">="), out t))
        {
            var s0 = Parse_additive(tokenizer);
        }
        var end = tokenizer.CurrentSpan.End;
        return new(start..end);
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new additive(statements, start..end);
    /// </code>
    /// </remarks>
    private partial additive Parse_additive(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_term(tokenizer);
        Helper.ParseAny(ParseT, tokenizer, t => t is not { Token: Token.Symbol { Value: "+" or "-" }});
        var end = tokenizer.CurrentSpan.End;
        return new(start..end);

        term ParseT(Tokenizer tokenizer)
        {
            if (Helper.TryConsume(tokenizer, new Token.Symbol("+"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("-"), out t))
            {
                return Parse_term(tokenizer);
            }
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        }
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new term(statements, start..end);
    /// </code>
    /// </remarks>
    private partial term Parse_term(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_unary(tokenizer);
        Helper.ParseAny(ParseU, tokenizer, t => t is not { Token: Token.Symbol { Value: "*" or "/" }});
        var end = tokenizer.CurrentSpan.End;
        return new(start..end);

        unary ParseU(Tokenizer tokenizer)
        {
            if (Helper.TryConsume(tokenizer, new Token.Symbol("*"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("/"), out t))
            {
                return Parse_unary(tokenizer);
            }
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        }
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new unary(statements, start..end);
    /// </code>
    /// </remarks>
    private partial unary Parse_unary(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        if (Helper.TryConsume(tokenizer, new Token.Symbol("+"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("-"), out t))
        {
            var s = Parse_unary(tokenizer);
        }
        else
        {
            var s = Parse_exponentiation(tokenizer);
        }
        var end = tokenizer.PreviousSpan.End;
        return new(start..end);
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new exponentiation(statements, start..end);
    /// </code>
    /// </remarks>
    private partial exponentiation Parse_exponentiation(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s0 = Parse_postfix(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("^"), out var t))
        {
            var s1 = Parse_exponentiation(tokenizer);
        }
        var end = tokenizer.CurrentSpan.End;
        return new(start..end);
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new postfix(statements, start..end);
    /// </code>
    /// </remarks>
    private partial postfix Parse_postfix(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s0 = Parse_primary(tokenizer);
        var s1 = Helper.ParseAny(ParseA, tokenizer, t => t is not { Token: Token.Symbol { Value: "!" or "(" }});
        var end = tokenizer.CurrentSpan.End;
        return new(start..end);
        args ParseA(Tokenizer tokenizer)
        {
            if (Helper.TryConsume(tokenizer, new Token.Symbol("!"), out var t))
            {
                return default!;
            }

            if (Helper.TryConsume(tokenizer, new Token.Symbol("("), out t))
            {
                var a = Parse_args(tokenizer);
                Helper.Expect(tokenizer, new Token.Symbol(")"));
                return a;
            }
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        }
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new primary(statements, start..end);
    /// </code>
    /// </remarks>
    private partial TreeNode Parse_primary(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        if (Helper.TryConsume(tokenizer, new Token.Symbol("("), out var t))
        {
            var e = Parse_expression(tokenizer);
            Helper.Expect(tokenizer, new Token.Symbol(")"));
            return e with { Span = start..tokenizer.PreviousSpan.End };
        }
        if (Helper.TryConsume(tokenizer, new Token.Id(), out t))
            return new primary(start..tokenizer.PreviousSpan.End);
        if (Helper.TryConsume(tokenizer, new Token.Int(), out t))
            return new primary(start..tokenizer.PreviousSpan.End);
        if (Helper.TryConsume(tokenizer, new Token.String(), out t))
            return new primary(start..tokenizer.PreviousSpan.End);
        throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
    }

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
    /// var end = tokenizer.PreviousSpan.End;
    /// return new args(statements, start..end);
    /// </code>
    /// </remarks>
    private partial args Parse_args(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_expression(tokenizer);
        Helper.ParseAny(ParseE, tokenizer, t => t is not { Token: Token.Symbol { Value: "," }});
        var end = tokenizer.CurrentSpan.End;
        return new(start..end);

        TreeNode ParseE(Tokenizer tokenizer)
        {
            if (Helper.TryConsume(tokenizer, new Token.Symbol(","), out var t))
            {
                return Parse_expression(tokenizer);
            }
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        }
    }
}
