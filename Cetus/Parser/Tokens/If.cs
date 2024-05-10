namespace Cetus.Parser.Tokens;

public class If : ISpecialCharacterToken<If>
{
	public static string SpecialToken => "if";
	
	public string TokenText { get; init; } = null!;
}