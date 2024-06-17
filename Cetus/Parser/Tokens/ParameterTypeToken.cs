using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class ParameterTypeToken(string name) : IToken
{
	public Result Eat(Lexer lexer)
	{
		throw new InvalidOperationException("Parameter token was not contextualized");
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order) => new ParameterTypeTokenContextualized(name, arguments);
	
	public override string ToString() => $"${name}";
}

public class ParameterTypeTokenContextualized(string name, FunctionArgs arguments) : IToken
{
	public Result Eat(Lexer lexer)
	{
		Result valueResult = lexer.Eat(out TypeIdentifier value);
		if (valueResult is Result.Passable)
			arguments[name] = value;
		return valueResult;
	}
	
	public override string ToString() => $"${name}";
}