
namespace RecursiveParsing;

public partial interface IVisitor
{
    void Enter(statement statement) {}
    void Visit(statement statement) {}
    void Exit(statement statement) {}
    void Enter(blockstatement blockstatement) {}
    void Visit(blockstatement blockstatement) {}
    void Exit(blockstatement blockstatement) {}
    void Enter(expressionstatement expressionstatement) {}
    void Visit(expressionstatement expressionstatement) {}
    void Exit(expressionstatement expressionstatement) {}
    void Enter(expression expression) {}
    void Visit(expression expression) {}
    void Exit(expression expression) {}
    void Enter(conditionnal conditionnal) {}
    void Visit(conditionnal conditionnal) {}
    void Exit(conditionnal conditionnal) {}
    void Enter(equation equation) {}
    void Visit(equation equation) {}
    void Exit(equation equation) {}
    void Enter(relational relational) {}
    void Visit(relational relational) {}
    void Exit(relational relational) {}
    void Enter(additive additive) {}
    void Visit(additive additive) {}
    void Exit(additive additive) {}
    void Enter(term term) {}
    void Visit(term term) {}
    void Exit(term term) {}
    void Enter(unary unary) {}
    void Visit(unary unary) {}
    void Exit(unary unary) {}
    void Enter(exponentiation exponentiation) {}
    void Visit(exponentiation exponentiation) {}
    void Exit(exponentiation exponentiation) {}
    void Enter(postfix postfix) {}
    void Visit(postfix postfix) {}
    void Exit(postfix postfix) {}
    void Enter(primary primary) {}
    void Visit(primary primary) {}
    void Exit(primary primary) {}
    void Enter(args args) {}
    void Visit(args args) {}
    void Exit(args args) {}
}
