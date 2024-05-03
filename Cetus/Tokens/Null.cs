namespace Cetus.Tokens;

public class Null : ISpecialCharacterToken<Null>
{
	public static string SpecialToken => "null";
	
	public string TokenText { get; init; } = null!;
}