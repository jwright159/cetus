namespace Cetus.Tokens;

public class Newline : ISpecialCharacterToken<Newline>
{
	public static string SpecialToken => "\n";
	
	public string TokenText { get; init; } = null!;
}