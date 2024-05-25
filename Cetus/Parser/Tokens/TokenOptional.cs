namespace Cetus.Parser.Tokens;

public class TokenOptional(IToken token) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
}