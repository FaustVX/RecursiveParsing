
namespace RecursiveParsing;

public abstract partial record class TreeNode(Range Span)
{
    public abstract void Accept(IVisitor visitor);
}

/// <summary>
/// <code>statement := blockstatement ";"</code>
/// </summary>
public partial record class statement(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>blockstatement := "{" statement* "}" | expressionstatement</code>
/// </summary>
public partial record class blockstatement(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>expressionstatement := expression ";"</code>
/// </summary>
public partial record class expressionstatement(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>expression := conditionnal</code>
/// </summary>
public partial record class expression(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>conditionnal := equation "?" expression ":" conditionnal</code>
/// </summary>
public partial record class conditionnal(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>equation := relational (("==" | "!=") relational)?</code>
/// </summary>
public partial record class equation(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>relational := additive (("&lt;" | "&gt;" | "&lt;=" | "&gt;=") additive)?</code>
/// </summary>
public partial record class relational(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>additive := term (("+" | "-") term)*</code>
/// </summary>
public partial record class additive(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>term := unary (("*" | "/") unary)*</code>
/// </summary>
public partial record class term(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>unary := ("+" | "-") unary | exponentiation</code>
/// </summary>
public partial record class unary(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>exponentiation := postfix ("^" exponentiation)?</code>
/// </summary>
public partial record class exponentiation(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>postfix := primary ("!" | "(" args ")")*</code>
/// </summary>
public partial record class postfix(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>primary := ID | NUMBER | STRING | "(" expression ")"</code>
/// </summary>
public partial record class primary(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}

/// <summary>
/// <code>args := (expression ("," expression)*)?</code>
/// </summary>
public partial record class args(Range Span) : TreeNode(Span)
{
    public override void Accept(IVisitor visitor)
    {
        visitor.Enter(this);
        visitor.Visit(this);
        visitor.Exit(this);
    }
}
