using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class ParameterStatementToken(string name) : IToken
{
	public Result Eat(Lexer lexer)
	{
		throw new InvalidOperationException("Parameter token was not contextualized");
	}
	
	public IToken Contextualize(IHasIdentifiers context, Args arguments, int order) => new ParameterStatementTokenContextualized(name, context, arguments, order);
	
	public override string ToString() => $"${name}";
}

public class ParameterStatementTokenContextualized(string name, IHasIdentifiers context, Args arguments, int order) : IToken
{
	public Result Eat(Lexer lexer)
	{
		FunctionCall functionCall = new(context, order);
		Result functionCallResult = lexer.Eat(functionCall);
		if (functionCallResult is Result.Passable)
			arguments[name] = functionCall;
		return functionCallResult;
	}
	
	public override string ToString() => $"${name}";
}