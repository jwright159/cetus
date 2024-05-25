namespace Cetus.Parser.Tokens;

public class EOFToken : IToken
{
	public bool Eat(string contents, ref int index)
	{
		return index >= contents.Length;
	}
}