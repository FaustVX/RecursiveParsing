#nullable enable
using RecursiveParsing.Parse;

namespace RecursiveParsing.Visitors;

public partial class TreePrintVisitor(ReadOnlyMemory<char> input) : IVisitor
{
    private int _depth = 0;
    private static readonly Dictionary<int, string> _indent = [];
    protected string IndentSpaces(int depth)
    {
        ref var indent = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 2]);
        s.Fill(' ');
        return indent = new string(s);
    }
    protected void PrintTree(ReadOnlySpan<char> input, TreeNode node, bool isTerminal)
    => Console.WriteLine($"{IndentSpaces(_depth)}{node.GetType().Name} = [{node.Span}]{input[node.Span]}{(isTerminal ? "" : ":")}");

    void IVisitor.Enter(RecursiveParsing.Parse.PrefixExpr elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.PrefixExpr elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.PostfixExpr elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.PostfixExpr elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.BinaryExpr elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.BinaryExpr elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.TernaryExpr elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.TernaryExpr elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.CallExpr elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.CallExpr elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.ExpressionStatement elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.ExpressionStatement elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.BlockStatement elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.BlockStatement elem)
    {
        _depth--;
    }

    void IVisitor.Enter(RecursiveParsing.Parse.File elem)
    {
        PrintTree(input.Span, elem, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(RecursiveParsing.Parse.File elem)
    {
        _depth--;
    }
}
