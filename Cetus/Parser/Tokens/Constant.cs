namespace Cetus.Parser.Tokens;

public class Constant : ISpecialCharacterToken<Constant>
{
	public static string SpecialToken => "const";
	
	public string TokenText { get; init; } = null!;
}