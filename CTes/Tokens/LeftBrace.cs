namespace CTes.Tokens;

public class LeftBrace : ISpecialCharacterToken<LeftBrace>
{
	public static string SpecialToken => "{";
	
	public string TokenText { get; init; } = null!;
}