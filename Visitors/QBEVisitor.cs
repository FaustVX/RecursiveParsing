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
        QBEFile.AppendLine($$"""
        # static int bool_arginfo(const struct printf_info *info, size_t n, int *argtypes, int *size)
        # {
        #     if (n) {
        #         argtypes[0] = PA_INT;
        #         *size = sizeof(bool);
        #     }
        #     return 1;
        # }
        function w $__bool_arginfo(l %info, l %n, l %argstypes, l %size){
        @start
            jnz %n, @true, @false
        @true
            %pa_int =w copy 1
            storew %pa_int, %argstypes
            storew %pa_int, %size
        @false
            ret 1
        }

        # static int bool_printf(FILE *stream, const struct printf_info *info, const void *const *args)
        # {
        #     bool b =  *(const bool*)(args[0]);
        #     int r = fputs(b ? "true" : "false", stream);
        #     return r == EOF ? -1 : (b ? 4 : 5);
        # }
        function w $__bool_printf(l %stream, l %info, l %args){
        @start
            %args =l loadl %args
            %b =w loaduw %args
            %value =l call $_ternary_l(w %b, l $_true_literal, l $_false_literal)
            %r =w call $fputs(l %value, l %stream)
            %len =w call $strlen(l %value)
            %_1 =w copy -1
            %b =w ceqw %r, %_1
            %r =w call $_ternary_w(w %b, w -1, w %len)
            ret %r
        }

        # static int setup_bool_specifier()
        # {
        #     int r = register_printf_specifier('B', bool_printf, bool_arginfo);
        #     return r;
        # }
        function $__setup_bool_specifier() {
        @start
            %b =w copy 98
            %printf =l copy $__bool_printf
            %arginfo =l copy $__bool_arginfo
            %_ =w call $register_printf_specifier(w %b, l %printf, l %arginfo)
            jnz %_, @true, @false
        @false
            ret
        @true
            hlt
        }

        # char* concat(const char *s1, const char *s2)
        # {
        #     char *result = malloc(strlen(s1) + strlen(s2) + 1); // +1 for the null-terminator
        #     // in real code you would check for errors in malloc here
        #     strcpy(result, s1);
        #     strcat(result, s2);
        #     return result;
        # }
        function l $_strconcat(l %lhs, l %rhs) {
        @start
            %len =l call $strlen(l %lhs)
            %len1 =l call $strlen(l %rhs)
            %len =l add %len, %len1
            %len1 =l copy 1
            %len =l add %len, %len1
            %m =l call $malloc(l %len)
            %_ =l call $strcpy(l %m, l %lhs)
            %_ =l call $strcat(l %m, l %rhs)
            ret %m
        }

        function w $_strcmp(l %lhs, l %rhs) {
        @start
            %cmp =w call $strcmp(l %lhs, l %rhs)
            %val =w copy 0
            %cmp =w ceqw %cmp, %val
            ret %cmp
        }

        function w $_ternary_w(w %test, w %true, w %false) {
        @start
            jnz %test, @true, @false
        @true
            ret %true
        @false
            ret %false
        }

        function l $_ternary_l(w %test, l %true, l %false) {
        @start
            jnz %test, @true, @false
        @true
            ret %true
        @false
            ret %false
        }

        function $_println_int(w %int) {
        @start
            %fmt =l call $_strconcat(l $_int_fmt, l $_newline)
            call $printf(l %fmt, ..., w %int)
            call $free(l %fmt)
            ret
        }

        function $_println_str(l %str) {
        @start
            %fmt =l call $_strconcat(l $_str_fmt, l $_newline)
            call $printf(l %fmt, ..., w %str)
            call $free(l %fmt)
            ret
        }

        function $_println_bool(w %bool) {
        @start
            %fmt =l call $_strconcat(l $_bool_fmt, l $_newline)
            call $printf(l %fmt, ..., w %bool)
            call $free(l %fmt)
            ret
        }

        function $_call_n_w(l %fun, w %arg1) {
        @start
            call %fun(w %arg1)
            ret
        }

        function $_call_n_l(l %fun, l %arg1) {
        @start
            call %fun(l %arg1)
            ret
        }
        """).AppendLine().AppendLine($$"""
        export function w $main() {
        @start
            call $__setup_bool_specifier()
        """);
        QBEData.AppendLine("""data $_newline = { b "\n", b 0 }""")
        .AppendLine("""data $_int_fmt = { b "%d", b 0 }""")
        .AppendLine("""data $_str_fmt = { b "%s", b 0 }""")
        .AppendLine("""data $_bool_fmt = { b "%b", b 0 }""")
        .AppendLine("""data $_true_literal = { b "true", b 0 }""")
        .AppendLine("""data $_false_literal = { b "false", b 0 }""");
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
