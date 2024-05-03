namespace Cetus.Tokens;

public class Else : ISpecialCharacterToken<Else>
{
	public static string SpecialToken => "else";
	
	public string TokenText { get; init; } = null!;
}