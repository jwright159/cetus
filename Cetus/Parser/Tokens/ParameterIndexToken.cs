namespace Cetus.Parser.Tokens;

public class ParameterIndexToken(int index) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
	
	public string? TokenText { get; set; }
	
	public int Index => index;
}