namespace Cetus.Parser.Tokens;

public class Word : IToken
{
	public string Value { get; private set; }
	
	public Result Eat(Lexer lexer)
	{
		int startIndex = lexer.Index;
		
		if (char.IsLetter(lexer.Current) || lexer.Current == '_')
		{
			while (!lexer.IsAtEnd && (char.IsLetterOrDigit(lexer.Current) || lexer.Current == '_')) lexer.Index++;
			Value = lexer[startIndex..lexer.Index];
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected word, got {lexer.Current}", lexer, startIndex);
	}
}