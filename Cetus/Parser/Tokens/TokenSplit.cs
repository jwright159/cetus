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
		
		bool lookingForDelim = false; // If false, looking for token
		while (true)
		{
			// Start by checking for the end
			Result endResult = lexer.Eat(end);
			if (endResult is Result.Passable)
			{
				results.Add(endResult);
				break;
			}
			
			// If we're looking for a token, start with that
			// Doesn't matter if it fails or not, we'll just go to the next delim or end
			if (!lookingForDelim)
			{
				Result tokenResult = lexer.Eat(token);
				results.Add(tokenResult);
				lookingForDelim = true;
				continue;
			}
			
			// Otherwise, if we're looking for a delimiter, do that instead
			lookingForDelim = false;
			Result delimResult = lexer.Eat(delim);
			if (delimResult is Result.Passable)
			{
				results.Add(delimResult);
				continue;
			}
			
			// Oops, didn't find a delim, skip ahead to the next delim or end
			int skipStartIndex = lexer.Index;
			int skipStartLine = lexer.Line;
			int skipStartColumn = lexer.Column;
			bool shouldBreak = false;
			bool shouldContinue = false;
			while (!lexer.IsAtEnd)
			{
				int skipEndLine = lexer.Line;
				int skipEndColumn = lexer.Column;
				
				delimResult = lexer.Eat(delim);
				if (delimResult is Result.Passable)
				{
					results.Add(new Result.TokenRuleFailed($"Skipped to delimeter {delim} at {skipEndLine}:{skipEndColumn}", skipStartLine, skipStartColumn));
					shouldContinue = true;
					break;
				}
				
				endResult = lexer.Eat(end);
				if (endResult is Result.Passable)
				{
					results.Add(new Result.TokenRuleFailed($"Skipped to end token {end} at {skipEndLine}:{skipEndColumn}", skipStartLine, skipStartColumn));
					shouldBreak = true;
					break;
				}
				
				if (!lexer.EatAnyMatches())
					lexer.Index++;
			}
			
			if (shouldBreak) break;
			if (shouldContinue) continue;
			
			results.Add(new Result.TokenRuleFailed($"Expected delimiter or end, found {lexer[skipStartIndex]}, skipped to EOF", skipStartLine, skipStartColumn));
			break;
		}
		
		return Result.WrapPassable($"Expected token split \"{this}\"", results.ToArray());
	}
	
	public IToken Contextualize(IHasIdentifiers context, FunctionArgs arguments, int order, float priorityThreshold) =>
		new TokenSplit(
			start.Contextualize(context, arguments, order, priorityThreshold),
			delim.Contextualize(context, arguments, order, priorityThreshold),
			end.Contextualize(context, arguments, order, priorityThreshold),
			token.Contextualize(context, arguments, order, priorityThreshold));
	
	public override string ToString() => $"{start} ({token} {delim})* {end}";
}