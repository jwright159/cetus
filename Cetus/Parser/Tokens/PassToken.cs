namespace Cetus.Parser.Tokens;

public class PassToken : IToken
{
	public Result Eat(Lexer lexer)
	{
		return new Result.Ok();
	}
	
	public override string ToString() => "[true]";
}