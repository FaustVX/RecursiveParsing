using System.Collections.Immutable;

namespace EBNFParser.Phases.Parse;

public interface IVisitor
{
    void Enter(File file) {}
    void Visit(File file) {}
    void Exit(File file) {}
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

public abstract record class TreeNode(Range Span)
{
    public abstract void Accept(IVisitor visitor);
}

/// <summary>
/// file := declaration*
/// </summary>
public record class File(ImmutableArray<Declaration> Declarations, Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        for (int i = 0; i < Declarations.Length; i++)
        {
            if (i > 0)
                visitor.Visit(this);
            Declarations[i].Accept(visitor);
        }

        visitor.Exit(this);
    }
}

/// <summary>
/// declaration := ID ":=" expression EOL+
/// </summary>
public record class Declaration(Primary Id, Expression Expression, Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        Id.Accept(visitor);
        visitor.Visit(this);
        Expression.Accept(visitor);
        visitor.Exit(this);
    }
}
