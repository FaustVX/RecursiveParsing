using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using EBNFParser.Phases.Parse;

namespace EBNFParser.Visitors;

[Serializable]
public abstract class CheckIdentifierVisitorException : Exception;

[Serializable]
public class AlreadyReferencedIdentifierException(Id id) : CheckIdentifierVisitorException
{
    public Id Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is already defined";
}

[Serializable]
public class InexistantIdentifierException(Id id) : CheckIdentifierVisitorException
{
    public Id Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is inexistant";
}

[Serializable]
public class UnreadIdentifierException(Id id) : CheckIdentifierVisitorException
{
    public Id Id { get; } = id;

    public override string Message => $"\"{Id.Name}\" at [{Id.Span}] is unread";
}

sealed class CheckIdentifierVisitor : IVisitor
{
    private readonly HashSet<Id> _identifiers = [with(IdEqualityComparer.Instance)];
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
        if (Exceptions.Count > 0)
            throw new AggregateException([..Exceptions]);
    }

    private sealed class CheckInxistantId(FrozenSet<Id> identifiers, List<CheckIdentifierVisitorException> exceptions) : IVisitor
    {
        void IVisitor.Visit(Id id)
        {
            if (!identifiers.Contains(id))
                exceptions.Add(new InexistantIdentifierException(id));

        }
    }

    private sealed class CheckUnreadId : IVisitor
    {
        private bool _isDeclerationId = true;
        public HashSet<Id> ReferencedIds { get; } = [with(IdEqualityComparer.Instance)];
        void IVisitor.Enter(Declaration declaration)
        {
            _isDeclerationId = true;
        }
        void IVisitor.Visit(Id id)
        {
            if (!_isDeclerationId)
                ReferencedIds.Add(id);

        }
        void IVisitor.Visit(Declaration declaration)
        {
            _isDeclerationId = false;
        }
    }

    private sealed class IdEqualityComparer : IEqualityComparer<Id>
    {
        public static IdEqualityComparer Instance { get; } = new IdEqualityComparer();
        public bool Equals(Id? x, Id? y)
        => x?.Name == y?.Name;

        public int GetHashCode([DisallowNull] Id obj)
        => obj.Name.GetHashCode();
    }
}
