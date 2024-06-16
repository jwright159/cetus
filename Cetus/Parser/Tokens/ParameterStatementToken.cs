using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class ParameterStatementToken(string name) : IToken
{
	public Result Eat(Lexer lexer)
	{
		throw new InvalidOperationException("Parameter token was not contextualized");
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order, float priorityThreshold) => new ParameterStatementTokenContextualized(name, context, arguments, order, priorityThreshold);
	
	public override string ToString() => $"${name}";
}

public class ParameterStatementTokenContextualized(string name, IHasIdentifiers context, FunctionArgs arguments, int order, float priorityThreshold) : IToken
{
	public Result Eat(Lexer lexer)
	{
		FunctionCall functionCall = new(context, order, priorityThreshold);
		Result functionCallResult = lexer.Eat(functionCall);
		if (functionCallResult is Result.Passable)
			arguments[name] = functionCall;
		return functionCallResult;
	}
	
	public override string ToString() => $"${name}";
}