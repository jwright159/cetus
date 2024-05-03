namespace Cetus.Tokens;

public class Return : ISpecialCharacterToken<Return>
{
	public static string SpecialToken => "return";
	
	public string TokenText { get; init; } = null!;
}