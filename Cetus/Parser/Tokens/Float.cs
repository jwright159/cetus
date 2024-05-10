using System.Diagnostics.CodeAnalysis;

namespace Cetus.Parser.Tokens;

public class Float : IToken
{
	public static bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (char.IsDigit(contents[index]))
		{
			int i;
			bool dot = false;
			for (i = index; i < contents.Length && (char.IsDigit(contents[i]) || contents[i] == '.'); i++)
			{
				if (contents[i] == '.')
				{
					if (dot)
					{
						i--;
						break;
					}
					else
						dot = true;
				}
			}
			
			if (contents[i] == 'f')
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