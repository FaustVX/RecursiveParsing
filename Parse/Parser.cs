using System.Runtime.CompilerServices;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Parse;
sealed partial class Parser
{
    public File ParseFile()
    {
        Tokenizer.ScanToken();
        var start = Tokenizer.CurrentSpan.Start;
        var tree = Parse(Tokenizer);
        var end = Tokenizer.PreviousSpan.End;
        Helper.Expect(Tokenizer, new Token.EOF());
        return new([tree], start..end);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Statement Parse(Tokenizer tokenizer)
        {
            try
            {
                return Parse_Statement(tokenizer);
            }
            catch (EBNFException ex)
            {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
            }
        }
    }

    private partial Statement Parse_Statement(Tokenizer tokenizer)
    {
        if (tokenizer.CurrentToken is Token.Symbol { Value: "{" })
            return Parse_BlockStatement(tokenizer);
        if (tokenizer.CurrentToken is Token.Id { Value: "if" })
            return Parse_BranchStatement(tokenizer);
        return Parse_ExpressionStatement(tokenizer);
    }

    private partial IfStatement Parse_BranchStatement(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Helper.Expect(tokenizer, new Token.Id("if"));
        var condition = Parse_Expression(tokenizer);
        var @then = Parse_Statement(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Id("else")))
        {
            var @else = Parse_Statement(tokenizer);
            return new(condition, @then, @else, start..tokenizer.PreviousSpan.End);
        }
        return new(condition, @then, null, start..tokenizer.PreviousSpan.End);
    }

    private partial BlockStatement Parse_BlockStatement(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Helper.Expect(tokenizer, new Token.Symbol("{"));
        var s = Helper.ParseAny(Parse_Statement, tokenizer, t => t is { Token: Token.Symbol { Value: "}" }});
        Helper.Expect(tokenizer, new Token.Symbol("}"));
        var end = tokenizer.PreviousSpan.End;
        return new(s, start..end);
    }

    private partial ExpressionStatement Parse_ExpressionStatement(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var s = Parse_Expression(tokenizer);
        Helper.Expect(tokenizer, new Token.Symbol(";"));
        var end = tokenizer.PreviousSpan.End;
        return new(s, start..end);
    }

    private partial Expression Parse_Expression(Tokenizer tokenizer)
    => Parse_Conditionnal(tokenizer);

    private partial Expression Parse_Conditionnal(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var expr = Parse_Equation(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("?"), out var l))
        {
            var s1 = Parse_Expression(tokenizer);
            Helper.Expect(tokenizer, new Token.Symbol(":"), out var r);
            var s2 = Parse_Conditionnal(tokenizer);
            var end = tokenizer.PreviousSpan.End;
            return new TernaryExpr(expr, l, s1, r, s2, start..end) { Precedence = ExpressionPrecedence.Conditionnal };
        }
        return expr;
    }

    private partial Expression Parse_Equation(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var expr = Parse_Relational(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("=="), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("!="), out t))
        {
            var right = Parse_Equation(tokenizer);
            var end = tokenizer.PreviousSpan.End;
            return new BinaryExpr(expr, t, right, start..end) { Precedence = ExpressionPrecedence.Equation };
        }
        return expr;
    }

    private partial Expression Parse_Relational(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var expr = Parse_Additive(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("<"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("<="), out t) || Helper.TryConsume(tokenizer, new Token.Symbol(">"), out t) || Helper.TryConsume(tokenizer, new Token.Symbol(">="), out t))
        {
            var right = Parse_Relational(tokenizer);
            var end = tokenizer.PreviousSpan.End;
            return new BinaryExpr(expr, t, right, start..end) { Precedence = ExpressionPrecedence.Relational };
        }
        return expr;
    }

    private partial Expression Parse_Additive(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var expr = Parse_Term(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("+"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("-"), out t))
        {
            var right = Parse_Additive(tokenizer);
            var end = tokenizer.PreviousSpan.End;
            return new BinaryExpr(expr, t, right, start..end) { Precedence = ExpressionPrecedence.Additive };
        }
        return expr;
    }

    private partial Expression Parse_Term(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        var expr = Parse_Unary(tokenizer);
        if (Helper.TryConsume(tokenizer, new Token.Symbol("*"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("/"), out t))
        {
            var right = Parse_Term(tokenizer);
            var end = tokenizer.PreviousSpan.End;
            return new BinaryExpr(expr, t, right, start..end) { Precedence = ExpressionPrecedence.Term };
        }
        return expr;
    }

    private partial Expression Parse_Unary(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        if (Helper.TryConsume(tokenizer, new Token.Symbol("+"), out var t) || Helper.TryConsume(tokenizer, new Token.Symbol("-"), out t) || Helper.TryConsume(tokenizer, new Token.Symbol("!"), out t))
        {
            var right = Parse_Unary(tokenizer);
            var end = tokenizer.PreviousSpan.End;
            return new PrefixExpr(t, right, start..end) { Precedence = ExpressionPrecedence.Unary };
        }
        return Parse_Postfix(tokenizer);
    }

    private partial Expression Parse_Postfix(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        Expression expr = Parse_Primary(tokenizer);
        var s1 = Helper.ParseAny(ParseA, tokenizer, t => t is not { Token: Token.Symbol { Value: "!" or "(" }});
        return expr;
        Expression ParseA(Tokenizer tokenizer)
        {
            if (Helper.TryConsume(tokenizer, new Token.Symbol("("), out var t))
            {
                if (Helper.TryConsume(tokenizer, new Token.Symbol(")")))
                    return expr = new CallExpr(expr, [], start..tokenizer.CurrentSpan.End) { Precedence = ExpressionPrecedence.Postfix };
                var a = Parse_Args(tokenizer);
                Helper.Expect(tokenizer, new Token.Symbol(")"));
                var end = tokenizer.CurrentSpan.End;
                return expr = new CallExpr(expr, a, start..end) { Precedence = ExpressionPrecedence.Postfix };
            }
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        }
    }

    private partial Expression Parse_Primary(Tokenizer tokenizer)
    {
        var start = tokenizer.CurrentSpan.Start;
        if (Helper.TryConsume(tokenizer, new Token.Symbol("("), out var t))
        {
            var e = Parse_Expression(tokenizer);
            Helper.Expect(tokenizer, new Token.Symbol(")"));
            return e with { Span = start..tokenizer.PreviousSpan.End };
        }
        if (Helper.TryConsume(tokenizer, new Token.Id(), out t))
            return new Primary(start..tokenizer.PreviousSpan.End) { TokenSpan = t, Precedence = ExpressionPrecedence.Primary };
        if (Helper.TryConsume(tokenizer, new Token.Int(), out t))
            return new Primary(start..tokenizer.PreviousSpan.End) { TokenSpan = t, Precedence = ExpressionPrecedence.Primary };
        if (Helper.TryConsume(tokenizer, new Token.String(), out t))
            return new Primary(start..tokenizer.PreviousSpan.End) { TokenSpan = t, Precedence = ExpressionPrecedence.Primary };
        throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
    }

    private partial System.Collections.Immutable.ImmutableArray<Expression> Parse_Args(Tokenizer tokenizer)
    {
        var s = Parse_Expression(tokenizer);
        return [s, ..Helper.ParseAny(ParseE, tokenizer, t => t is not { Token: Token.Symbol { Value: "," }})];

        Expression ParseE(Tokenizer tokenizer)
        {
            if (Helper.TryConsume(tokenizer, new Token.Symbol(","), out var t))
            {
                return Parse_Expression(tokenizer);
            }
            throw new ParserUnexpectedException(tokenizer.CurrentTokenSpan);
        }
    }
}
