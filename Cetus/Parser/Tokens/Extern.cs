namespace Cetus.Parser.Tokens;

public class Extern : ISpecialCharacterToken<Extern>
{
	public static string SpecialToken => "extern";
	
	public string TokenText { get; init; } = null!;
}