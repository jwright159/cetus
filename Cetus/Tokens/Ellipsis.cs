namespace CTes.Tokens;

public class Ellipsis : ISpecialCharacterToken<Ellipsis>
{
	public static string SpecialToken => "...";
	
	public string TokenText { get; init; } = null!;
}