namespace Cetus.Parser.Tokens;

public class LeftBrace : ISpecialCharacterToken<LeftBrace>
{
	public static string SpecialToken => "{";
	
	public string TokenText { get; init; } = null!;
}

public class RightBrace : ISpecialCharacterToken<RightBrace>
{
	public static string SpecialToken => "}";
	
	public string TokenText { get; init; } = null!;
}