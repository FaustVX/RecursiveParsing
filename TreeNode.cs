using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Text;

namespace RecursiveParsing;

public readonly union RTObject(decimal, bool, Delegate)
{
    public static RTObject FromObject(object obj)
    => obj switch
    {
        RTObject o => o,
        int i => i,
        float f => (decimal)f,
        double d => (decimal)d,
        decimal d => d,
        bool b => b,
        Delegate d => d,
        _ => throw new RunTimeException(),
    };
    public override string ToString()
    => Value?.ToString() ?? "null";
}

[Serializable]
public class RunTimeException() : Exception;

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
    public FrozenDictionary<string, RTObject> Variables { get; } = variables.ToFrozenDictionary();
}
