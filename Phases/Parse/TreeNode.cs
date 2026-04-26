using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;

namespace RecursiveParsing.Phases.Parse;

[Serializable]
public class RunTimeException() : Exception;

[Serializable]
public class UnknownVariableRTException(string name) : RunTimeException
{
    public string Name { get; } = name;

    public override string ToString()
    => $"Unknown name: {Name}\n" + base.ToString();
}

public interface IVisitor
{
    void Enter(File file);
    void Exit(File file);
    void Enter(Declaration declaration);
    void Exit(Declaration declaration);
    void Enter(Choice choice);
    void Exit(Choice choice);
    void Enter(Sequence sequence);
    void Exit(Sequence sequence);
    void Enter(Postfix postfix);
    void Exit(Postfix postfix);
    void Visit(String @string);
    void Visit(Id id);
    void Visit(Terminal terminal);
}

public abstract record class TreeNode(Range Span)
{
    public abstract void Print(StringBuilder sb);
    public abstract void PrintTree(ReadOnlySpan<char> input, int indentation = 0);
    private static readonly Dictionary<int, string> _indent = [];
    protected string IndentSpaces(int depth)
    {
        ref var indent = ref CollectionsMarshal.GetValueRefOrAddDefault(_indent, depth, out var exists);
        if (exists)
            return indent!;
        var s = (stackalloc char[depth * 2]);
        s.Fill(' ');
        return indent = new string(s);
    }
    protected void PrintTreeImpl(ReadOnlySpan<char> input, int indentation, bool isTerminal)
    => Console.WriteLine($"{IndentSpaces(indentation)}{GetType().Name} = [{Span}]{input[Span]}{(isTerminal ? "" : ":")}");
    public abstract void Accept(IVisitor visitor);
}

/// <summary>
/// file := declaration*
/// </summary>
public record class File(ImmutableArray<Declaration> Declarations, Range Span) : TreeNode(Span)
{
    public override void Print(StringBuilder sb)
    {
        for (var i = 0; i < Declarations.Length; i++)
            Declarations[i].Print(sb);
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        foreach (var decl in Declarations)
            decl.PrintTree(input, indentation + 1);
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        foreach (var decl in Declarations)
            decl.Accept(visitor);
        visitor.Exit(this);
    }
}

/// <summary>
/// declaration := ID ":=" expression EOL+
/// </summary>
public record class Declaration(Id Id, Expression Expression, Range Span) : TreeNode(Span)
{
    public override void Print(StringBuilder sb)
    {
        Id.Print(sb);
        sb.Append(" := ");
        Expression.Print(sb);
        sb.AppendLine();
    }

    public override void PrintTree(ReadOnlySpan<char> input, int indentation)
    {
        PrintTreeImpl(input, indentation, isTerminal: false);
        Id.PrintTree(input, indentation + 1);
        Expression.PrintTree(input, indentation + 1);
    }

    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Id.Accept(visitor);
        Expression.Accept(visitor);
        visitor.Exit(this);
    }
}
