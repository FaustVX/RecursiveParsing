using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RecursiveParsing;

public readonly union RTObject(Delegate)
{
    public static RTObject FromObject(object obj)
    => obj switch
    {
        RTObject o => o,
        Delegate d => d,
        _ => throw new RunTimeException(),
    };
    public override string ToString()
    => Value?.ToString() ?? "null";
}

[Serializable]
public class RunTimeException() : Exception;

[Serializable]
public class UnknownVariableRTException(string name) : RunTimeException
{
    public string Name { get; } = name;

    public override string ToString()
    => $"Unknown name: {Name}\n" + base.ToString();
}

public abstract record class TreeNode(Range Span)
{
    private static readonly Dictionary<int, string> _indent = [];
    public abstract void Print(StringBuilder sb);
    public abstract void PrintTree(ReadOnlySpan<char> input, int indentation = 0);
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
}

public class Context(params IEnumerable<KeyValuePair<string, RTObject>> variables)
{
    public Context? Outer { get; init; }
    private readonly FrozenDictionary<string, RTObject> _variables = variables.ToFrozenDictionary();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RTObject Get(string name)
    => _variables.TryGetValue(name, out var value) ? value : Outer?.Get(name) ?? throw new UnknownVariableRTException(name);
}

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
}

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
}
