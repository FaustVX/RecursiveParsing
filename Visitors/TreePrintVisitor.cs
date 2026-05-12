using RecursiveParsing.Parse;

namespace RecursiveParsing.Visitors;

partial class TreePrintVisitor
{
    void IVisitor.Visit(Primary primary)
    {
        PrintTree(input.Span, primary, isTerminal: true);
    }
}
