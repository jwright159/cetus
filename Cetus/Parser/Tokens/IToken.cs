namespace Cetus.Parser.Tokens;

public interface IToken
{
	public bool Eat(string contents, ref int index);
	public string? TokenText { get; set; }
}