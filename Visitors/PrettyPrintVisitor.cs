using System.Collections.Immutable;
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
    private int _nodeCnt;
    void IVisitor.Enter(Phases.Parse.File file)
    {
        _nodeCnt = file.Nodes.Length;
    }
    void IVisitor.Visit(Phases.Parse.File file)
    {
        if (--_nodeCnt == 0)
            StringBuilder.AppendLine();
        StringBuilder.AppendLine();
    }
    void IVisitor.Exit(Phases.Parse.File file)
    {
        StringBuilder.AppendLine();
    }

    private bool _isDectarationNodeType;
    void IVisitor.Enter(Declaration declaration)
    {
        Debug.Assert(_precedences.Count == 1);
        _isDectarationNodeType = declaration.Node is not null;
    }

    void IVisitor.Visit(Declaration declaration)
    {
        if (_isDectarationNodeType)
            StringBuilder.Append(" : ");
        else
            StringBuilder.Append(" := ");
        _isDectarationNodeType = false;
    }

    void IVisitor.Exit(Declaration declaration)
    {
        _isDectarationNodeType = false;
        Debug.Assert(_precedences.Count == 1);
    }

    private Queue<string> _nodeTokens = [];
    void IVisitor.Enter(Node node)
    {
        Debug.Assert(_precedences.Count == 1);

        if (node.Params.Length is 0)
            _nodeTokens.Enqueue(" : ");
        else
        {
            _nodeTokens.Enqueue(" (");
            for (var i = 1; i < node.Params.Length; i++)
                _nodeTokens.Enqueue(", ");
            _nodeTokens.Enqueue(") : ");
        }

        if (node.Args.Length is not 0)
        {
            _nodeTokens.Enqueue(" (");
            for (var i = 1; i < node.Args.Length; i++)
                _nodeTokens.Enqueue(", ");
            _nodeTokens.Enqueue(")");
        }
    }

    void IVisitor.Visit(Node node)
    {
        if (_nodeTokens.TryDequeue(out var t))
            StringBuilder.Append(t);
    }

    void IVisitor.Exit(Node node)
    {
        if (_nodeTokens.TryDequeue(out var t))
            StringBuilder.Append(t);
        _nodeTokens.Clear();
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
            Token.String => StringBuilder.Append('"').Append(Token.Escape(primary.Name)).Append('"'),
            _ => throw new UnreachableException(),
        };
    }
}
