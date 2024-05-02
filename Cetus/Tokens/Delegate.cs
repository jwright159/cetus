namespace Cetus.Tokens;

public class Delegate : ISpecialCharacterToken<Delegate>
{
	public static string SpecialToken => "delegate";
	
	public string TokenText { get; init; } = null!;
}