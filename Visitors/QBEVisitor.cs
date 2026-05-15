using System.Diagnostics;
using System.Text;
using RecursiveParsing.Parse;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Visitors;

// https://c9x.me/compile/

public sealed class QBEVisitor : Visitor
{
    public StringBuilder QBEFile = new();

    private int _varCount = 0;

    public override void Enter(Parse.File file)
    {
        Debug.Assert(_varCount == 0);
        QBEFile.AppendLineLinux($$"""
        export function w $main() {
        @start
        """);
    }

    public override void Exit(Parse.File file)
    {
        Debug.Assert(_varCount == 0);
        QBEFile.AppendLineLinux($$"""
            ret 0
        }
        data $int = { b "%d\n", b 0 }
        """);
    }

    public override void Exit(CallExpr callExpr)
    {
        switch (callExpr)
        {
            case { Expression: Primary { TokenSpan.Token: Token.Id { Value: "printf" } }, Args.Length: 1 }:
                Debug.Assert(_varCount >= 1);
                QBEFile.AppendLineLinux($$"""    call $printf(l $int, ..., w %_w{{_varCount - 1}})""");
                _varCount -= 1;
            break;
        }
    }

    public override void Exit(BinaryExpr binaryExpr)
    {
        Debug.Assert(_varCount >= 2);
        switch (binaryExpr.Operator.Token)
        {
            case Token.Symbol { Value: "+" }:
                QBEFile.AppendLineLinux($$"""    %_w{{_varCount - 2}} =w add %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            case Token.Symbol { Value: "-" }:
                QBEFile.AppendLineLinux($$"""    %_w{{_varCount - 2}} =w sub %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            case Token.Symbol { Value: "*" }:
                QBEFile.AppendLineLinux($$"""    %_w{{_varCount - 2}} =w mul %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            case Token.Symbol { Value: "/" }:
                QBEFile.AppendLineLinux($$"""    %_w{{_varCount - 2}} =w div %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            default: throw new UnreachableException();
        }
        _varCount -= 1;
    }

    public override void Exit(PrefixExpr prefixExpr)
    {
        Debug.Assert(_varCount >= 1);
        switch (prefixExpr.Operator.Token)
        {
            case Token.Symbol { Value: "+" }:
                break;
            case Token.Symbol { Value: "-" }:
                QBEFile.AppendLineLinux($$"""    %_w{{_varCount - 1}} =w neg %_w{{_varCount - 1}}""");
                break;
            default: throw new UnreachableException();
        }
    }

    public override void Visit(Primary primary)
    {
        switch (primary.TokenSpan.Token)
        {
            case Token.String { Value: string s }:
                break;
            case Token.Id { Value: string i }:
                // QBEFile.AppendLineLinux($$"""    %_l{{_varCount}} =l copy {{i}}""");
                break;
            case Token.Int { Value: int i }:
                QBEFile.AppendLineLinux($$"""    %_w{{_varCount}} =w copy {{i}}""");
                _varCount += 1;
                break;
            default: throw new UnreachableException();
        }
    }
}
