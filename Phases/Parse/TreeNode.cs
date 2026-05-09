using System.Collections;
using System.Collections.Immutable;

namespace EBNFParser.Phases.Parse;

public interface IVisitor
{
    void Enter(File file) {}
    void Visit(File file) {}
    void Exit(File file) {}
    void Enter(Node node) {}
    void Visit(Node node) {}
    void Exit(Node node) {}
    void Enter(Declaration declaration) {}
    void Visit(Declaration declaration) {}
    void Exit(Declaration declaration) {}
    void Enter(Choice choice) {}
    void Visit(Choice choice) {}
    void Exit(Choice choice) {}
    void Enter(Sequence sequence) {}
    void Visit(Sequence sequence) {}
    void Exit(Sequence sequence) {}
    void Enter(Postfix postfix) {}
    void Exit(Postfix postfix) {}
    void Visit(Primary primary) {}
}

public abstract record class TreeNode(Range Span) : IStructuralEquatable
{
    public abstract void Accept(IVisitor visitor);
    protected static bool StructuralEquals<T>(T lhs, T? rhs)
    where T : struct, IStructuralEquatable
    => rhs?.Equals(lhs, EqualityComparer.Instance) ?? false;
    protected static bool StructuralEquals<T>(T lhs, T? rhs)
    where T : class, IStructuralEquatable
    => rhs?.Equals(lhs, EqualityComparer.Instance) ?? false;
    public bool Equals(object? other, IEqualityComparer comparer)
    => other is TreeNode node && comparer.Equals(this, node);
    public int GetHashCode(IEqualityComparer comparer)
    => comparer.GetHashCode(this);

    public sealed class EqualityComparer : IEqualityComparer
    {
        private EqualityComparer() {}
        public static EqualityComparer Instance { get; set; } = new();

        public new bool Equals(object? x, object? y)
        => (x, y) switch
        {
            (File lhs, File rhs) => lhs.Equals(rhs),
            (Node lhs, Node rhs) => lhs.Equals(rhs),
            (Declaration lhs, Declaration rhs) => lhs.Equals(rhs),
            (Choice lhs, Choice rhs) => lhs.Equals(rhs),
            (Sequence lhs, Sequence rhs) => lhs.Equals(rhs),
            (Postfix lhs, Postfix rhs) => lhs.Equals(rhs),
            (Primary lhs, Primary rhs) => lhs.Equals(rhs),
            _ => false,
        };

        public int GetHashCode(object obj)
        => obj.GetHashCode();
    }
}

/// <summary>
/// file := node+ EOL+ declaration+
/// </summary>
public record class File(ImmutableArray<Node> Nodes, ImmutableArray<Declaration> Declarations, Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        for (int i = 0; i < Nodes.Length; i++)
        {
            if (i > 0)
                visitor.Visit(this);
            Nodes[i].Accept(visitor);
        }
        for (int i = 0; i < Declarations.Length; i++)
        {
            visitor.Visit(this);
            Declarations[i].Accept(visitor);
        }

        visitor.Exit(this);
    }

    public virtual bool Equals(File? other)
    => StructuralEquals(Nodes, other?.Nodes) && StructuralEquals(Declarations, other?.Declarations);

    public override int GetHashCode()
    => HashCode.Combine(Nodes, Declarations);
}

/// <summary>
/// node := ID call? ":" ID call? EOL
/// </summary>
public record class Node(Primary Id, ImmutableArray<Expression> Params, Primary Inherit, ImmutableArray<Expression> Args, Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Id.Accept(visitor);
        for (int i = 0; i < Params.Length; i++)
        {
            visitor.Visit(this);
            Params[i].Accept(visitor);
        }
        visitor.Visit(this);
        Inherit.Accept(visitor);
        for (int i = 0; i < Args.Length; i++)
        {
            visitor.Visit(this);
            Args[i].Accept(visitor);
        }
        visitor.Exit(this);
    }

    public virtual bool Equals(Node? other)
    => (other?.Id.Equals(Id) ?? false) && StructuralEquals(Params, other?.Params) && (other?.Inherit.Equals(Inherit) ?? false) && StructuralEquals(Args, other?.Args);

    public override int GetHashCode()
    => HashCode.Combine(Id, Params, Inherit, Args);
}

/// <summary>
/// declaration := ID (":" ID)? ":=" expression EOL
/// </summary>
public record class Declaration(Primary Id, Primary? Node, Expression Expression, Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Id.Accept(visitor);
        visitor.Visit(this);
        if (Node is {} node)
        {
            node.Accept(visitor);
            visitor.Visit(this);
        }
        Expression.Accept(visitor);
        visitor.Exit(this);
    }

    public virtual bool Equals(Declaration? other)
    => (other?.Id.Equals(Id) ?? false) && (other?.Node?.Equals(Node) ?? (Node, other) is (null, { Node: null })) && (other?.Expression.Equals(Expression) ?? false);

    public override int GetHashCode()
    => HashCode.Combine(Id, Node, Expression);
}
