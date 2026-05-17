#nullable enable
using RecursiveParsing.Parse;

namespace RecursiveParsing.Visitors;

public partial class TreeTypePrintVisitor(ReadOnlyMemory<char> input) : Visitor
{
    private int _depth = 0;
    private static readonly Dictionary<int, string> _indent = [];
#pragma warning disable CS0628 // New protected member declared in sealed type
    protected string IndentSpaces(int depth)
    {
        ref var indent = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 2]);
        s.Fill(' ');
        return indent = new string(s);
    }
    protected void PrintTree(ReadOnlySpan<char> input, Expression expression, bool isTerminal)
#pragma warning restore CS0628 // New protected member declared in sealed type
    => Console.WriteLine($"{IndentSpaces(_depth)}{expression.GetType().Name} = [{(expression is BinaryExpr b ? string.Join(" or ", b.Signatures) : expression.Type)}]{input[expression.Span]}{(isTerminal ? ";" : ":")}");

    public override void Visit(Primary primary)
    {
        PrintTree(input.Span, primary, isTerminal: true);
    }

    public override void Enter(PrefixExpr prefixExpr)
    {
        PrintTree(input.Span, prefixExpr, isTerminal: false);
        _depth++;
    }

    public override void Exit(PrefixExpr prefixExpr)
    {
        _depth--;
    }

    public override void Enter(PostfixExpr postfixExpr)
    {
        PrintTree(input.Span, postfixExpr, isTerminal: false);
        _depth++;
    }

    public override void Exit(PostfixExpr postfixExpr)
    {
        _depth--;
    }

    public override void Enter(BinaryExpr binaryExpr)
    {
        PrintTree(input.Span, binaryExpr, isTerminal: false);
        _depth++;
    }

    public override void Exit(BinaryExpr binaryExpr)
    {
        _depth--;
    }

    public override void Enter(TernaryExpr ternaryExpr)
    {
        PrintTree(input.Span, ternaryExpr, isTerminal: false);
        _depth++;
    }

    public override void Exit(TernaryExpr ternaryExpr)
    {
        _depth--;
    }

    public override void Enter(CallExpr callExpr)
    {
        PrintTree(input.Span, callExpr, isTerminal: false);
        _depth++;
    }

    public override void Exit(CallExpr callExpr)
    {
        _depth--;
    }
}
