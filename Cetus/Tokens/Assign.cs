namespace Cetus.Tokens;

public class Assign : ISpecialCharacterToken<Assign>
{
	public static string SpecialToken => "=";
	
	public string TokenText { get; init; } = null!;
}