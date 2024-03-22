namespace CTes;

public class Parser(IEnumerable<Token> tokens)
{
	private Queue<Token> queue = new(tokens);
	private Stack<Expression> expressions = [];
	
	public static IEnumerable<Expression> Parse(IEnumerable<Token> tokens)
	{
		Parser parser = new(tokens);
		
		while (parser.queue.Count > 0)
		{
			parser.expressions.Push(parser.ParseExpression());
		}
		
		return parser.expressions;
	}
	
	private Expression ParseExpression()
	{
		Token token = queue.Dequeue();
		
		if (token.Type == TokenType.Number)
		{
			return new NumberExpression(double.Parse(token.Value));
		}
		
		if (token.Type == TokenType.Identifier)
		{
			return new IdentifierExpression(token.Value);
		}
		
		if (token.Type == TokenType.LeftParenthesis)
		{
			Expression expression = ParseExpression();
			queue.Dequeue();
			return expression;
		}
		
		if (token.Type == TokenType.Operator)
		{
			Expression left = expressions.Pop();
			Expression right = ParseExpression();
			return new BinaryExpression(left, right, token);
		}
		
		throw new Exception($"Unexpected token: {token}");
	}
}