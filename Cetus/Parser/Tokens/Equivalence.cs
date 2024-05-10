namespace Cetus.Parser.Tokens;

public class Equivalence : ISpecialCharacterToken<Equivalence>
{
	public static string SpecialToken => "==";
	
	public string TokenText { get; init; } = null!;
}

public class Inequivalence : ISpecialCharacterToken<Inequivalence>
{
	public static string SpecialToken => "!=";
	
	public string TokenText { get; init; } = null!;
}