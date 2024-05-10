namespace Cetus.Parser.Tokens;

public class Struct : ISpecialCharacterToken<Struct>
{
	public static string SpecialToken => "struct";
	
	public string TokenText { get; init; } = null!;
}