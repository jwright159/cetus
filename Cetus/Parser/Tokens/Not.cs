namespace Cetus.Parser.Tokens;

public class Not : ISpecialCharacterToken<Not>
{
	public static string SpecialToken => "!";
	
	public string TokenText { get; init; } = null!;
}