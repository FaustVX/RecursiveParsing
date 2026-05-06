using System.Diagnostics;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

public class TreePrintVisitor(ReadOnlyMemory<char> input) : IVisitor
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
    void IVisitor.Enter(Phases.Parse.File file)
    {
        Debug.Assert(_depth == 0);
        PrintTree(input.Span, file, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Phases.Parse.File declaration)
    {
        _depth--;
        Debug.Assert(_depth == 0);
    }
    void IVisitor.Enter(Declaration declaration)
    {
        Debug.Assert(_depth == 1);
        PrintTree(input.Span, declaration, isTerminal: false);
        _depth++;
    }
    void IVisitor.Exit(Declaration declaration)
    {
        _depth--;
        Debug.Assert(_depth == 1);
    }

    void IVisitor.Enter(Choice choice)
    {
        Debug.Assert(_depth >= 2);
        PrintTree(input.Span, choice, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(Choice choice)
    {
        _depth--;
        Debug.Assert(_depth >= 2);
    }

    void IVisitor.Enter(Sequence sequence)
    {
        Debug.Assert(_depth >= 2);
        PrintTree(input.Span, sequence, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(Sequence sequence)
    {
        _depth--;
        Debug.Assert(_depth >= 2);
    }

    void IVisitor.Enter(Postfix postfix)
    {
        Debug.Assert(_depth >= 2);
        PrintTree(input.Span, postfix, isTerminal: false);
        _depth++;
    }

    void IVisitor.Exit(Postfix postfix)
    {
        _depth--;
        Debug.Assert(_depth >= 2);
    }

    void IVisitor.Visit(Primary primary)
    {
        Debug.Assert(_depth >= 2);
        PrintTree(input.Span, primary, isTerminal: true);
    }
}
