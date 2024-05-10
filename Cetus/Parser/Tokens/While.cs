namespace Cetus.Parser.Tokens;

public class While : ISpecialCharacterToken<While>
{
	public static string SpecialToken => "while";
	
	public string TokenText { get; init; } = null!;
}