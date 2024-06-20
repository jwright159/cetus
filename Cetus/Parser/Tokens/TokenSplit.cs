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
			if (TryEnd())
				break;
			
			if (!lookingForDelim)
				TryToken();
			
			else if (!TryDelim() && SkipToDelimOrEnd())
				break;
		}
		
		return Result.WrapPassable($"Expected token split \"{this}\"", results.ToArray());
		
		
		bool TryEnd()
		{
			Result endResult = lexer.Eat(end);
			if (endResult is Result.Passable)
			{
				results.Add(endResult);
				return true;
			}
			return false;
		}
		
		bool TryToken()
		{
			// Doesn't matter if it fails or not, we'll just go to the next delim or end
			lookingForDelim = true;
			Result tokenResult = lexer.Eat(token);
			results.Add(tokenResult);
			return true;
		}
		
		bool TryDelim()
		{
			lookingForDelim = false;
			Result delimResult = lexer.Eat(delim);
			if (delimResult is Result.Passable)
			{
				results.Add(delimResult);
				return true;
			}
			return false;
		}
		
		// If true, found end
		bool SkipToDelimOrEnd()
		{
			int skipStartIndex = lexer.Index;
			int skipStartLine = lexer.Line;
			int skipStartColumn = lexer.Column;
			while (!lexer.IsAtEnd)
			{
				int skipEndLine = lexer.Line;
				int skipEndColumn = lexer.Column;
				
				Result delimResult = lexer.Eat(delim);
				if (delimResult is Result.Passable)
				{
					results.Add(new Result.TokenRuleFailed($"Skipped to delimeter {delim} at {skipEndLine}:{skipEndColumn}", skipStartLine, skipStartColumn));
					return false;
				}
				
				Result endResult = lexer.Eat(end);
				if (endResult is Result.Passable)
				{
					results.Add(new Result.TokenRuleFailed($"Skipped to end token {end} at {skipEndLine}:{skipEndColumn}", skipStartLine, skipStartColumn));
					return true;
				}
				
				if (!lexer.EatAnyMatches())
					lexer.Index++;
			}
			
			results.Add(new Result.TokenRuleFailed($"Expected delimiter or end, found {lexer[skipStartIndex]}, skipped to EOF", skipStartLine, skipStartColumn));
			return true;
		}
	}
	
	public IToken Contextualize(IHasIdentifiers context, Args arguments, int order) =>
		new TokenSplit(
			start.Contextualize(context, arguments, order),
			delim.Contextualize(context, arguments, order),
			end.Contextualize(context, arguments, order),
			token.Contextualize(context, arguments, order));
	
	public override string ToString() => $"{start} ({token} {delim})* {end}";
}