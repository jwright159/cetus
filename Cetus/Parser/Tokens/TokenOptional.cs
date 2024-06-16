using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class TokenOptional(IToken token) : IToken
{
	public Result Eat(Lexer lexer)
	{
		if (lexer.Eat(token) is Result.Passable result)
			return result;
		return new Result.Ok();
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order, float priorityThreshold) => new TokenOptional(token.Contextualize(context, arguments, order, priorityThreshold));
	
	public override string ToString() => token + "?";
}