namespace Cetus.Parser.Tokens;

public class Reference : ISpecialCharacterToken<Reference>
{
	public static string SpecialToken => "&";
	
	public string TokenText { get; init; } = null!;
}

public class Dereference : ISpecialCharacterToken<Dereference>
{
	public static string SpecialToken => "*";
	
	public string TokenText { get; init; } = null!;
}