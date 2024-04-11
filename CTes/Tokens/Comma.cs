namespace CTes.Tokens;

public class Comma : ISpecialCharacterToken<Comma>
{
	public static string SpecialToken => ",";
	
	public string TokenText { get; init; } = null!;
}