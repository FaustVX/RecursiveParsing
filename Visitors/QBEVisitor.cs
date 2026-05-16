using System.Diagnostics;
using System.Text;
using RecursiveParsing.Parse;
using RecursiveParsing.Tokenize;

namespace RecursiveParsing.Visitors;

// https://c9x.me/compile/

public sealed class QBEVisitor : Visitor
{
    public StringBuilder QBEFile = new();
    public StringBuilder QBEData = new();

    private int _varCount = 0;
    private int _strCount = 0;

    public override void Enter(Parse.File file)
    {
        Debug.Assert(_varCount == 0);
        QBEFile.AppendLine($$"""
        export function w $main() {
        @start
        """);
        QBEData.AppendLine("""data $int = { b "%d\n", b 0 }""")
        .AppendLine("""data $str = { b "%s\n", b 0 }""");
    }

    public override void Exit(Parse.File file)
    {
        Debug.Assert(_varCount == 0);
        QBEFile.AppendLine($$"""
            ret 0
        }
        """)
        .AppendLine()
        .Append(QBEData);
    }

    public override void Exit(CallExpr callExpr)
    {
        switch (callExpr)
        {
            case { Expression: Primary { TokenSpan.Token: Token.Id { Value: "printf" } }, Args: [Primary { TokenSpan.Token: Token.String }] }:
                Debug.Assert(_varCount >= 1);
                QBEFile.AppendLine($$"""    call $printf(l $str, ..., l %_l{{_varCount - 1}})""");
                _varCount -= 1;
            break;
            case { Expression: Primary { TokenSpan.Token: Token.Id { Value: "printf" } }, Args.Length: 1 }:
                Debug.Assert(_varCount >= 1);
                QBEFile.AppendLine($$"""    call $printf(l $int, ..., w %_w{{_varCount - 1}})""");
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
                QBEFile.AppendLine($$"""    %_w{{_varCount - 2}} =w add %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            case Token.Symbol { Value: "-" }:
                QBEFile.AppendLine($$"""    %_w{{_varCount - 2}} =w sub %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            case Token.Symbol { Value: "*" }:
                QBEFile.AppendLine($$"""    %_w{{_varCount - 2}} =w mul %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
                break;
            case Token.Symbol { Value: "/" }:
                QBEFile.AppendLine($$"""    %_w{{_varCount - 2}} =w div %_w{{_varCount - 2}}, %_w{{_varCount - 1}}""");
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
                QBEFile.AppendLine($$"""    %_w{{_varCount - 1}} =w neg %_w{{_varCount - 1}}""");
                break;
            default: throw new UnreachableException();
        }
    }

    public override void Visit(Primary primary)
    {
        switch (primary.TokenSpan.Token)
        {
            case Token.String { Value: string s }:
                QBEFile.AppendLine($$"""    %_l{{_varCount}} =l copy $str_{{_strCount}}""");
                QBEData.AppendLine($$"""data $str_{{_strCount++}} = { b "{{s}}", b 0 }""");
                _varCount += 1;
                break;
            case Token.Id { Value: string i }:
                break;
            case Token.Int { Value: int i }:
                QBEFile.AppendLine($$"""    %_w{{_varCount}} =w copy {{i}}""");
                _varCount += 1;
                break;
            default: throw new UnreachableException();
        }
    }
}
