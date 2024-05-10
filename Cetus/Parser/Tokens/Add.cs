namespace Cetus.Parser.Tokens;

public class Add : ISpecialCharacterToken<Add>
{
	public static string SpecialToken => "+";
	
	public string TokenText { get; init; } = null!;
}