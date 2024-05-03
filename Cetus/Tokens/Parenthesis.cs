namespace Cetus.Tokens;

public class LeftParenthesis : ISpecialCharacterToken<LeftParenthesis>
{
	public static string SpecialToken => "(";
	
	public string TokenText { get; init; } = null!;
}

public class RightParenthesis : ISpecialCharacterToken<RightParenthesis>
{
	public static string SpecialToken => ")";
	
	public string TokenText { get; init; } = null!;
}