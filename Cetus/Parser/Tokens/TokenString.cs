using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class TokenString(IToken[] tokens) : IToken
{
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		List<Result> results = [];
		
		foreach (IToken token in tokens)
		{
			Result result = lexer.Eat(token);
			results.Add(result);
			if (result is not Result.Passable)
			{
				lexer.Index = startIndex;
				return result;
			}
		}
		
		return Result.WrapPassable($"Expected token string \"{this}\"", results.ToArray());
	}
	
	public IToken Contextualize(IHasIdentifiers context, Args arguments, int order) => new TokenString(tokens.Select(token => token.Contextualize(context, arguments, order)).ToArray());
	
	public override string ToString() => string.Join(" ", tokens.Select(token => token.ToString()));
}