using System.Diagnostics;
using System.Text;
using EBNFParser.Phases.Parse;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Visitors;

public class PrettyPrintVisitor : IVisitor
{
    public StringBuilder StringBuilder { get; } = new();
    private readonly Stack<NodePrecedence> _precedences = [];
    private NodePrecedence LastPrecedence => _precedences.First();

    public PrettyPrintVisitor()
    {
        _precedences.Push((NodePrecedence)0);
    }

    private void Enter(Expression node)
    {
        if (node.Precedence < LastPrecedence)
            StringBuilder.Append('(');
        _precedences.Push(node.Precedence);
    }

    private void Exit(Expression node)
    {
        _precedences.Pop();
        if (node.Precedence < LastPrecedence)
            StringBuilder.Append(')');
    }
    void IVisitor.Visit(Phases.Parse.File file)
    {
        StringBuilder.AppendLine();
    }
    void IVisitor.Exit(Phases.Parse.File file)
    {
        StringBuilder.AppendLine();
    }

    void IVisitor.Enter(Declaration declaration)
    {
        Debug.Assert(_precedences.Count == 1);
    }

    void IVisitor.Visit(Declaration declaration)
    {
        StringBuilder.Append(" := ");
    }

    void IVisitor.Exit(Declaration declaration)
    {
        Debug.Assert(_precedences.Count == 1);
    }

    void IVisitor.Enter(Choice choice)
    {
        Enter(choice);
    }

    void IVisitor.Visit(Choice choice)
    {
        StringBuilder.Append(" | ");
    }

    void IVisitor.Exit(Choice choice)
    {
        Exit(choice);
    }

    void IVisitor.Enter(Sequence sequence)
    {
        Enter(sequence);
    }

    void IVisitor.Visit(Sequence sequence)
    {
        StringBuilder.Append(' ');
    }

    void IVisitor.Exit(Sequence sequence)
    {
        Exit(sequence);
    }

    void IVisitor.Enter(Postfix postfix)
    {
        Enter(postfix);
    }

    void IVisitor.Exit(Postfix postfix)
    {
        Exit(postfix);
        StringBuilder.Append(postfix.Operator.Token.TokenString());
    }

    void IVisitor.Visit(Primary primary)
    {
        _ = primary.TokenSpan.Token switch
        {
            Token.Id => StringBuilder.Append(primary.Name),
            Token.Terminal => StringBuilder.Append(primary.Name),
            Token.String => StringBuilder.Append('"').Append(Token.String.Escape(primary.Name)).Append('"'),
            _ => throw new UnreachableException(),
        };
    }
}
