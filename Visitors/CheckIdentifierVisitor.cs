using System.Collections.Frozen;
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
public class InexistantIdentifierException(Primary id) : CheckIdentifierVisitorException
{
    public override Range Range => Id.Span;
    public override string ErrorCode => "EB_0006";
    public override string SubCategory => "Inexistant id";

    public Primary Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is inexistant";
}

[Serializable]
public class UnreadIdentifierException(Primary id) : CheckIdentifierVisitorException
{
    public override Range Range => Id.Span;
    public override string ErrorCode => "EB_0007";
    public override string SubCategory => "Unread id";

    public Primary Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is unread";
}

public sealed class CheckIdentifierVisitor(bool throwOnError) : IVisitor
{
    private readonly HashSet<Primary> _identifiers = [with(IdEqualityComparer.Instance)];
    public List<CheckIdentifierVisitorException> Exceptions { get; } = [];

    void IVisitor.Enter(Declaration declaration)
    {
        if (!_identifiers.Add(declaration.Id))
            Exceptions.Add(new AlreadyReferencedIdentifierException(declaration.Id));
    }

    void IVisitor.Exit(Phases.Parse.File file)
    {
        file.Accept(new CheckInxistantId(_identifiers.ToFrozenSet(IdEqualityComparer.Instance), Exceptions));
        var inner = new CheckUnreadId();
        file.Accept(inner);
        foreach (var id in _identifiers)
            if (!inner.ReferencedIds.Contains(id))
                Exceptions.Add(new UnreadIdentifierException(id));
        if (throwOnError && Exceptions.Count > 0)
            throw new AggregateException([..Exceptions]);
    }

    private sealed class CheckInxistantId(FrozenSet<Primary> identifiers, List<CheckIdentifierVisitorException> exceptions) : IVisitor
    {
        void IVisitor.Visit(Primary id)
        {
            if (id.TokenSpan.Token is Token.Id && !identifiers.Contains(id))
                exceptions.Add(new InexistantIdentifierException(id));

        }
    }

    private sealed class CheckUnreadId : IVisitor
    {
        private bool _isDeclerationId = true;
        public HashSet<Primary> ReferencedIds { get; } = [with(IdEqualityComparer.Instance)];
        void IVisitor.Enter(Declaration declaration)
        {
            _isDeclerationId = true;
        }
        void IVisitor.Visit(Primary id)
        {
            if (id.TokenSpan.Token is Token.Id && !_isDeclerationId)
                ReferencedIds.Add(id);

        }
        void IVisitor.Visit(Declaration declaration)
        {
            _isDeclerationId = false;
        }
    }

    private sealed class IdEqualityComparer : IEqualityComparer<Primary>
    {
        public static IdEqualityComparer Instance { get; } = new IdEqualityComparer();
        public bool Equals(Primary? x, Primary? y)
        => x?.Name == y?.Name;

        public int GetHashCode([DisallowNull] Primary obj)
        => obj.Name.GetHashCode();
    }
}
