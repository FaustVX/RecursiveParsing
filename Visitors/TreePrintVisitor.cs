using RecursiveParsing.Parse;

namespace RecursiveParsing.Visitors;

sealed partial class TreePrintVisitor
{
    public override void Visit(Primary primary)
    {
        PrintTree(input.Span, primary, isTerminal: true);
    }
}
