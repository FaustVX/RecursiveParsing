using System.Collections.Frozen;
using System.Text;

namespace RecursiveParsing;

public abstract class TreeNode
{
    public abstract void Print(StringBuilder sb);
    public abstract decimal Evaluate(Context ctx);
}

public class Context(params IEnumerable<KeyValuePair<string, decimal>> variables)
{
    public FrozenDictionary<string, decimal> Variables { get; } = variables.ToFrozenDictionary();
}

public sealed class Number(decimal i) : TreeNode
{
    public decimal I { get; } = i;

    public override decimal Evaluate(Context ctx)
    => I;

    public override void Print(StringBuilder sb)
    => sb.Append(I);
}

public sealed class Id(string name) : TreeNode
{
    public string Name { get; } = name;

    public override decimal Evaluate(Context ctx)
    => ctx.Variables[Name];

    public override void Print(StringBuilder sb)
    => sb.Append(Name);
}

public abstract class UnaryNode(TreeNode node) : TreeNode
{
    public TreeNode Node { get; } = node;
}

public sealed class Negate(TreeNode node) : UnaryNode(node)
{
    public override decimal Evaluate(Context ctx)
    => -Node.Evaluate(ctx);

    public override void Print(StringBuilder sb)
    {
        sb.Append('-');
        Node.Print(sb);
    }
}

public abstract class BinaryNode(TreeNode left, TreeNode right) : TreeNode
{
    public TreeNode Left { get; } = left;
    public TreeNode Right { get; } = right;
}

public sealed class Add(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) + Right.Evaluate(ctx);

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('+');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Substract(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) - Right.Evaluate(ctx);

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('-');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Multiply(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) * Right.Evaluate(ctx);

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('*');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Divide(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override decimal Evaluate(Context ctx)
    => Left.Evaluate(ctx) / Right.Evaluate(ctx);

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('/');
        Right.Print(sb);
        sb.Append(')');
    }
}

public sealed class Power(TreeNode left, TreeNode right) : BinaryNode(left, right)
{
    public override decimal Evaluate(Context ctx)
    => (decimal)double.Pow((double)Left.Evaluate(ctx), (double)Right.Evaluate(ctx));

    public override void Print(StringBuilder sb)
    {
        sb.Append('(');
        Left.Print(sb);
        sb.Append('^');
        Right.Print(sb);
        sb.Append(')');
    }
}
