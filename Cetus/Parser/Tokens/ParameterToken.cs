namespace Cetus.Parser.Tokens;

public abstract class ParameterToken(string name) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		throw new NotImplementedException();
	}
	
	public string ParameterName => name;
	
	public override string ToString() => $"${name}";
}

public class ParameterExpressionToken(string name) : ParameterToken(name);

public class ParameterValueToken(string name) : ParameterToken(name);