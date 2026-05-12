#nullable enable
namespace RecursiveParsing.Parse;

public partial interface IVisitor
{
    void Enter(PrefixExpr primary) {}
    void Exit(PrefixExpr primary) {}
    void Enter(PostfixExpr primary) {}
    void Exit(PostfixExpr primary) {}
    void Enter(BinaryExpr primary) {}
    void Visit(BinaryExpr primary) {}
    void Exit(BinaryExpr primary) {}
    void Enter(TernaryExpr primary) {}
    void Visit(TernaryExpr primary) {}
    void Exit(TernaryExpr primary) {}
    void Enter(CallExpr primary) {}
    void Visit(CallExpr primary) {}
    void Exit(CallExpr primary) {}
    void Enter(ExpressionStatement primary) {}
    void Exit(ExpressionStatement primary) {}
    void Enter(BlockStatement primary) {}
    void Visit(BlockStatement primary) {}
    void Exit(BlockStatement primary) {}
    void Enter(File primary) {}
    void Visit(File primary) {}
    void Exit(File primary) {}
}
