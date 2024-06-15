using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class TokenOptions(IToken[] possibleTokens) : IToken
{
	public Result Eat(Lexer lexer)
	{
		foreach (IToken token in possibleTokens)
			if (lexer.Eat(token) is Result.Passable result)
				return result;
		return new Result.TokenRuleFailed($"Expected token options \"{this}\"", lexer);
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order) => new TokenOptions(possibleTokens.Select(token => token.Contextualize(context, arguments, order)).ToArray());
	
	public override string ToString() => "(" + string.Join(" | ", possibleTokens.AsEnumerable()) + ")";
}