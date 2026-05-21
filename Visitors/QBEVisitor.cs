using System.Collections.Immutable;
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
        // https://stackoverflow.com/a/22114373/5830999 bool for `printf` format
        QBEFile.AppendLine(System.IO.File.ReadAllText("Visitors/runtime.ssa"))
        .AppendLine($$"""
        export function w $main() {
        @start
            call $__setup_bool_specifier()
        """);
        QBEData.AppendLine("""data $_main_return_value = { w 0 }""");
    }

    public override void Exit(Parse.File file)
    {
        Debug.Assert(_varCount == 0);
        QBEFile.AppendLine($$"""
            %_main_return_value =w loadw $_main_return_value
            ret %_main_return_value
        }
        """)
        .AppendLine()
        .Append(QBEData);
    }

    public override void Exit(CallExpr callExpr)
    {
        switch (callExpr)
        {
            case { Type: ExpressionType.None, Args: [{Type: ExpressionType.String or ImmutableArray<FunctionSignature> { Length: 1 }}] }:
                QBEFile.AppendLine($$"""    call $_call_n_l(l %_l{{_varCount - 2}}, l %_l{{_varCount - 1}})""");
                _varCount -= 2;
                break;
            case { Type: ExpressionType.None, Args: [{Type: ExpressionType.Int or ExpressionType.Bool}] }:
                QBEFile.AppendLine($$"""    call $_call_n_w(l %_l{{_varCount - 2}}, w %_w{{_varCount - 1}})""");
                _varCount -= 2;
                break;
        }
    }

    public override void Exit(BinaryExpr binaryExpr)
    {
        Console.WriteLine(string.Join(" or ", binaryExpr.Signatures));
        Debug.Assert(_varCount >= 2);
        _ = binaryExpr.Signatures switch
        {
            { Length: not 1 } or [{ Name.Length: 0 } or { Args.Length: not 2 }] => throw new UnreachableException(),
            [{ Name: ['_', ..], Return: var ret, Args: [var lhs, var rhs] } sig] => QBEFile.AppendLine($$"""    %_{{TypeToQBE(ret)}}{{_varCount - 2}} ={{TypeToQBE(ret)}} call ${{sig.Name}}({{TypeToQBE(lhs)}} %_{{TypeToQBE(lhs)}}{{_varCount - 2}}, {{TypeToQBE(rhs)}} %_{{TypeToQBE(rhs)}}{{_varCount - 1}})"""),
            [{ Return: var ret, Args: [var lhs, var rhs] } sig] => QBEFile.AppendLine($$"""    %_{{TypeToQBE(ret)}}{{_varCount - 2}} ={{TypeToQBE(ret)}} {{sig.Name}} %_{{TypeToQBE(lhs)}}{{_varCount - 2}}, %_{{TypeToQBE(rhs)}}{{_varCount - 1}}"""),
        };
        _varCount -= 1;
    }

    static char TypeToQBE(ExpressionTypeUnion type)
    => type switch
    {
        ImmutableArray<FunctionSignature> or ExpressionType.String => 'l',
        ExpressionType.Bool or ExpressionType.Int => 'w',
        _ => throw new UnreachableException(),
    };

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

    public override void Exit(TernaryExpr ternaryExpr)
    {
        Debug.Assert(_varCount >= 3);
        switch (ternaryExpr.Type, ternaryExpr.OpLeft.Token)
        {
            case (ExpressionType.Int or ExpressionType.Bool, Token.Symbol { Value: "?" }):
                QBEFile.AppendLine($$"""    %_w{{_varCount - 3}} =w call $_ternary_w(w %_w{{_varCount - 3}}, w %_w{{_varCount - 2}}, w %_w{{_varCount - 1}})""");
                break;
            case (ExpressionType.String or ImmutableArray<FunctionSignature> { Length: 1 } or ImmutableArray<FunctionSignature> { Length: 1 }, Token.Symbol { Value: "?" }):
                QBEFile.AppendLine($$"""    %_l{{_varCount - 3}} =l call $_ternary_l(w %_w{{_varCount - 3}}, l %_l{{_varCount - 2}}, l %_l{{_varCount - 1}})""");
                break;
        }
        _varCount -= 2;
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
            case Token.Id when primary.Type is ImmutableArray<FunctionSignature> { Length: 1 } sig:
                QBEFile.AppendLine($$"""    %_l{{_varCount}} =l copy ${{sig[0].Name}}""");
                _varCount += 1;
                break;
            case Token.Int { Value: int i }:
                QBEFile.AppendLine($$"""    %_w{{_varCount}} =w copy {{i}}""");
                _varCount += 1;
                break;
            default: throw new UnreachableException();
        }
    }
}
