namespace Cetus.Parser.Tokens;

public abstract class ParameterToken(int index) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
	
	public string? TokenText { get; set; }
	
	public int Index => index;
}

public class ParameterExpressionToken(int index) : ParameterToken(index);

public class ParameterValueToken(int index) : ParameterToken(index);