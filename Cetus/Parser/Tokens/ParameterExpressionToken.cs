﻿using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class ParameterExpressionToken(string name) : IToken
{
	public Result Eat(Lexer lexer)
	{
		throw new InvalidOperationException("Parameter token was not contextualized");
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order) => new ParameterExpressionTokenContextualized(name, context, arguments, order);
	
	public override string ToString() => $"${name}";
}

public class ParameterExpressionTokenContextualized(string name, IHasIdentifiers context, FunctionArgs arguments, int order) : IToken
{
	public Result Eat(Lexer lexer)
	{
		Expression expression = new(context, order);
		Result expressionResult = lexer.Eat(expression);
		if (expressionResult is Result.Passable)
			arguments[name] = expression;
		return expressionResult;
	}
	
	public override string ToString() => $"${name}";
}