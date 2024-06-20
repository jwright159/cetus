using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class ParameterTypeToken(string name) : IToken
{
	public Result Eat(Lexer lexer)
	{
		throw new InvalidOperationException("Parameter token was not contextualized");
	}
	
	public IToken Contextualize(IHasIdentifiers context, Args arguments, int order) => new ParameterTypeTokenContextualized(name, context, arguments);
	
	public override string ToString() => $"${name}";
}

public class ParameterTypeTokenContextualized(string name, IHasIdentifiers context, Args arguments) : IToken
{
	public Result Eat(Lexer lexer)
	{
		TypeIdentifierCall call = new(context, 0);
		if (lexer.Eat(call) is Result.Passable callResult)
		{
			arguments[name] = call;
			return callResult;
		}
		
		if (lexer.Eat(out ValueIdentifier nameId) is Result.Passable nameResult)
		{
			arguments[name] = new TypeIdentifierName(nameId.Name);
			return nameResult;
		}
		
		return new Result.TokenRuleFailed("Expreted type identifier", lexer);
	}
	
	public override string ToString() => $"${name}";
}