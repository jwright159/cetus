namespace Cetus.Tokens;

public class Semicolon : ISpecialCharacterToken<Semicolon>
{
	public static string SpecialToken => ";";
	
	public string TokenText { get; init; } = null!;
}