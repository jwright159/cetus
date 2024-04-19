namespace Cetus.Tokens;

public class RightParenthesis : ISpecialCharacterToken<RightParenthesis>
{
	public static string SpecialToken => ")";
	
	public string TokenText { get; init; } = null!;
}