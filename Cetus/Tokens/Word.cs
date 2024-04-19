using System.Diagnostics.CodeAnalysis;

namespace Cetus.Tokens;

public class Word : IToken
{
	public static bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (char.IsLetter(contents[index]) || contents[index] == '_')
		{
			int i = index;
			while (i < contents.Length && (char.IsLetterOrDigit(contents[i]) || contents[i] == '_')) i++;
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