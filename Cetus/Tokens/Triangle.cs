namespace Cetus.Tokens;

public class LeftTriangle : ISpecialCharacterToken<LeftTriangle>
{
	public static string SpecialToken => "<";
	
	public string TokenText { get; init; } = null!;
}

public class RightTriangle : ISpecialCharacterToken<RightTriangle>
{
	public static string SpecialToken => ">";
	
	public string TokenText { get; init; } = null!;
}