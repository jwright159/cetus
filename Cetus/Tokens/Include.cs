using System.Diagnostics.CodeAnalysis;

namespace Cetus.Tokens;

public class Include : IToken
{
	public static bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (contents[index..].StartsWith("include"))
		{
			token = "include";
			index += "include".Length;
			return true;
		}
		else
		{
			token = null;
			return false;
		}
	}

	public string TokenText { get; init; } = null!;
}