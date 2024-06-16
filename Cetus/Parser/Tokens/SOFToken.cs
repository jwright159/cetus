namespace Cetus.Parser.Tokens;

public class SOFToken : IToken
{
	public Result Eat(Lexer lexer)
	{
		return lexer.Index == 0 ? new Result.Ok() : new Result.TokenRuleFailed($"Expected SOF, got {lexer.Current}", lexer);
	}
	
	public override string ToString() => "[SOF]";
}