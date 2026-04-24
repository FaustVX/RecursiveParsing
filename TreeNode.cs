using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RecursiveParsing;

public readonly union RTObject(decimal, bool, string, Delegate)
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
        string s => s,
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
