namespace Cetus.Parser.Tokens;

public class Ellipsis : ISpecialCharacterToken<Ellipsis>
{
	public static string SpecialToken => "...";
	
	public string TokenText { get; init; } = null!;
}