namespace Cetus.Tokens;

public class RightBrace : ISpecialCharacterToken<RightBrace>
{
	public static string SpecialToken => "}";
	
	public string TokenText { get; init; } = null!;
}