using System.Diagnostics.CodeAnalysis;

namespace Cetus.Parser.Tokens;

public class DecimalInteger : IToken
{
	public static bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (char.IsDigit(contents[index]))
		{
			int i = index;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
			token = contents[index..i];
			index = i;
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

public class HexInteger : IToken
{
	public static bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (contents.Length > index + 2 && contents[index..(index+2)] == "0x")
		{
			int i = index + 2;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
			token = contents[index..i];
			index = i;
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