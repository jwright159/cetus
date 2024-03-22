namespace CTes;

public class Expression;

public class NumberExpression : Expression
{
	public double Value { get; }
	
	public NumberExpression(double value)
	{
		Value = value;
	}
}

public class IdentifierExpression : Expression
{
	public string Name { get; }
	
	public IdentifierExpression(string name)
	{
		Name = name;
	}
}

public class BinaryExpression : Expression
{
	public Expression Left { get; }
	public Expression Right { get; }
	public Token Operator { get; }
	
	public BinaryExpression(Expression left, Expression right, Token op)
	{
		Left = left;
		Right = right;
		Operator = op;
	}
}