using System.Diagnostics.CodeAnalysis;

namespace Cetus.Tokens;

public class String : IToken
{
	public static bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (contents[index] == '"')
		{
			int i = index;
			while (i < contents.Length && contents[i] != '"') i++;
			
			if (contents[i] == '"')
				i++;
			else
			{
				token = null;
				return false;
			}
			
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