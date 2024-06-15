namespace Cetus.Parser.Tokens;

public class LiteralToken(string token) : IToken
{
	public Result Eat(Lexer lexer)
	{
		if (lexer[lexer.Index..].StartsWith(token))
		{
			lexer.Index += token.Length;
			return new Result.Ok();
		}
		
		return new Result.TokenRuleFailed($"Expected '{token}', got {lexer.Current}", lexer);
	}
	
	public override string ToString() => $"'{token}'";
}