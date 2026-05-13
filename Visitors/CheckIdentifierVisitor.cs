using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EBNFParser.Phases.Parse;
using EBNFParser.Phases.Tokenize;

namespace EBNFParser.Visitors;

[Serializable]
public abstract class CheckIdentifierVisitorException : EBNFException;

[Serializable]
public class AlreadyReferencedIdentifierException(Primary id) : CheckIdentifierVisitorException
{
    public override Range Range => Id.Span;
    public override string ErrorCode => "EB_0005";
    public override string SubCategory => "Already referenced id";

    public Primary Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is already defined";
}

[Serializable]
public class InexistantDeclarationIdentifierException(Primary id) : CheckIdentifierVisitorException
{
    public override Range Range => Id.Span;
    public override string ErrorCode => "EB_0006";
    public override string SubCategory => "Inexistant declaration";

    public Primary Id { get; } = id;

    public override string Message => $"Declaration id: \"{Id.Name}\" at [{Id.Span}] is inexistant";
}

[Serializable]
public class InexistantNodeIdentifierException(Primary id) : CheckIdentifierVisitorException
{
    public override Range Range => Id.Span;
    public override string ErrorCode => "EB_0007";
    public override string SubCategory => "Inexistant node";

    public Primary Id { get; } = id;

    public override string Message => $"Node id: \"{Id.Name}\" at [{Id.Span}] is inexistant";
}

[Serializable]
public class UnreadIdentifierException(Primary id) : CheckIdentifierVisitorException
{
    public override ExceptionLevel Level => ExceptionLevel.Warning;
    public override Range Range => Id.Span;
    public override string ErrorCode => "EB_0008";
    public override string SubCategory => "Unread id";

    public Primary Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is unread";
}

public sealed class CheckIdentifierVisitor(bool throwOnError) : IVisitor
{
    private readonly HashSet<Primary> _nodeIds = [new(new(new Token.Id("tree-node"), ..)), new(new(new Token.Id("token-span"), ..)),];
    private readonly HashSet<Primary> _identifiers = [];
    public List<CheckIdentifierVisitorException> Exceptions { get; } = [];

    void IVisitor.Enter(Phases.Parse.File file)
    {
        foreach (var n in file.Nodes)
        {
            if (!_nodeIds.Contains(n.Inherit))
                Exceptions.Add(new InexistantNodeIdentifierException(n.Inherit));
            if (!_nodeIds.Add((Primary)n.Id.Node))
                Exceptions.Add(new AlreadyReferencedIdentifierException((Primary)n.Id.Node));
        }
    }

    void IVisitor.Enter(Node node)
    {
        var props = new HashSet<Primary>(capacity: node.Params.Length + 2)
        {
            new(new(new Token.Id("tree-node"), ..)),
            new(new(new Token.Id("token-span"), ..)),
        };
        foreach (var p in node.Params)
            switch (p.Expressions)
            {
                case [Postfix { Node: Primary type }, Primary prop]:
                    Exceptions.AddRange(Check(_nodeIds, props, type, prop));
                    break;
                case [Primary type, Primary prop]:
                    Exceptions.AddRange(Check(_nodeIds, props, type, prop));
                    break;
                default: throw new UnreachableException();
            }
        foreach (var a in node.Args)
            if (!props.Contains(a))
                Exceptions.Add(new InexistantNodeIdentifierException(a));

        static IEnumerable<CheckIdentifierVisitorException> Check(HashSet<Primary> nodeIds, HashSet<Primary> props, Primary type, Primary prop)
        {
            if (!nodeIds.Contains(type))
                yield return new InexistantNodeIdentifierException(type);
            if (!props.Add(prop))
                yield return new AlreadyReferencedIdentifierException(prop);
        }
    }

    void IVisitor.Enter(Declaration declaration)
    {
        if (!_identifiers.Add(declaration.Id))
            Exceptions.Add(new AlreadyReferencedIdentifierException(declaration.Id));
        var node = declaration.Node switch
        {
            Primary p => p,
            Postfix { Node: Primary p } => p,
            _ => throw new UnreachableException(),
        };
        if (!_nodeIds.Contains(node))
            Exceptions.Add(new InexistantNodeIdentifierException(node));
    }

    void IVisitor.Exit(Phases.Parse.File file)
    {
        var identifiers = _identifiers.ToFrozenSet();
        var inexistant = new CheckInexistantId(identifiers, Exceptions);
        var unread = new CheckUnreadId();
        foreach (var d in file.Declarations)
        {
            d.Expression.Accept(inexistant);
            d.Expression.Accept(unread);
        }
        foreach (var id in identifiers)
            if (!unread.ReferencedIds.Contains(id))
                Exceptions.Add(new UnreadIdentifierException(id));
        if (throwOnError && Exceptions.Count > 0)
            throw new AggregateException([..Exceptions]);
    }

    private sealed class CheckInexistantId(FrozenSet<Primary> identifiers, List<CheckIdentifierVisitorException> exceptions) : IVisitor
    {
        void IVisitor.Visit(Primary id)
        {
            if (id.TokenSpan.Token is Token.Id && !identifiers.Contains(id))
                exceptions.Add(new InexistantDeclarationIdentifierException(id));
        }
    }

    private sealed class CheckUnreadId : IVisitor
    {
        public HashSet<Primary> ReferencedIds { get; } = [];
        void IVisitor.Visit(Primary id)
        {
            if (id.TokenSpan.Token is Token.Id)
                ReferencedIds.Add(id);
        }
    }
}
