namespace Cetus.Tokens;

public class Pound : ISpecialCharacterToken<Pound>
{
	public static string SpecialToken => "#";
	
	public string TokenText { get; init; } = null!;
}