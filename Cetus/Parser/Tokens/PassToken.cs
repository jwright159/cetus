namespace Cetus.Parser.Tokens;

public class PassToken : IToken
{
	public bool Eat(string contents, ref int index)
	{
		return true;
	}
}