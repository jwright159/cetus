namespace Cetus.Parser.Tokens;

public class TokenOptions(IToken[] possibleTokens) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
}