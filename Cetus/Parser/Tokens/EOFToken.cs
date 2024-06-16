namespace Cetus.Parser.Tokens;

public class EOFToken : IToken
{
	public Result Eat(Lexer lexer)
	{
		return lexer.IsAtEnd ? new Result.Ok() : new Result.TokenRuleFailed($"Expected EOF, got {lexer.Current}", lexer);
	}
	
	public override string ToString() => "[EOF]";
}