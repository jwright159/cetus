using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class ParameterValueToken(string name) : IToken
{
	public Result Eat(Lexer lexer)
	{
		throw new InvalidOperationException("Parameter token was not contextualized");
	}
	
	public IToken Contextualize(IHasIdentifiers context, Args arguments, int order) => new ParameterValueTokenContextualized(name, arguments);
	
	public override string ToString() => $"${name}";
}

public class ParameterValueTokenContextualized(string name, Args arguments) : IToken
{
	public Result Eat(Lexer lexer)
	{
		Result valueResult = lexer.Eat(out ValueIdentifier value);
		if (valueResult is Result.Passable)
			arguments[name] = value;
		return valueResult;
	}
	
	public override string ToString() => $"${name}";
}