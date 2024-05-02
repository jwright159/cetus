namespace Cetus.Tokens;

public class Dereference : ISpecialCharacterToken<Dereference>
{
	public static string SpecialToken => "*";
	
	public string TokenText { get; init; } = null!;
}