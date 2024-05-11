namespace Cetus.Parser.Tokens;

public class String : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (contents[index] == '"')
		{
			int i;
			for (i = index + 1; i < contents.Length && contents[i] != '"'; i++)
			{
				if (contents[i] == '\\')
					i++;
			}
			
			if (contents[i] == '"')
				i++;
			else
			{
				TokenText = null;
				return false;
			}
			
			TokenText = contents[index..i];
			index = i;
			return true;
		}
		else
		{
			TokenText = null;
			return false;
		}
	}
	
	public string? TokenText { get; set; }
}