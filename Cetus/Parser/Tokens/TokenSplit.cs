using Cetus.Parser.Types;

namespace Cetus.Parser.Tokens;

public class TokenSplit(IToken start, IToken delim, IToken end, IToken token) : IToken
{
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		List<Result> results = [];
		
		Result startResult = lexer.Eat(start);
		results.Add(startResult);
		if (startResult is not Result.Passable)
		{
			lexer.Index = startIndex;
			return startResult;
		}
		
		while (true)
		{
			Result tokenResult = lexer.Eat(token);
			if (tokenResult is not Result.Passable)
				break;
			results.Add(tokenResult);
			
			Result delimResult = lexer.Eat(delim);
			if (delimResult is not Result.Passable)
				break;
			results.Add(delimResult);
		}
		
		Result endResult = lexer.SkipToMatches(end)!;
		results.Add(endResult);
		if (endResult is not Result.Passable)
		{
			lexer.Index = startIndex;
			return endResult;
		}
		
		return Result.WrapPassable($"Expected token split \"{this}\"", results.ToArray());
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order) =>
		new TokenSplit(
			start.Contextualize(context, arguments, order),
			delim.Contextualize(context, arguments, order),
			end.Contextualize(context, arguments, order),
			token.Contextualize(context, arguments, order));
	
	public override string ToString() => $"{start} ({token} {delim})* {end}";
}