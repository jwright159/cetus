namespace Cetus.Parser.Tokens;

public class TokenSplit(IToken start, IToken delim, IToken end, IToken token) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
}