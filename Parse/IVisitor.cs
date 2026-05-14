namespace RecursiveParsing.Parse;

partial interface IVisitor
{
    void Visit(Primary primary);
}

partial class Visitor
{
    public virtual void Visit(Primary primary) {}
}
