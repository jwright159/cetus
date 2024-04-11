namespace CTes.Tokens;

public class LeftParenthesis : ISpecialCharacterToken<LeftParenthesis>
{
	public static string SpecialToken => "(";
	
	public string TokenText { get; init; } = null!;
}