namespace Cetus.Parser.Tokens;

public class TokenString(IToken[] tokens) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
}