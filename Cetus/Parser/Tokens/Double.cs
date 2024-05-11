namespace Cetus.Parser.Tokens;

public class Double : IToken
{
	public bool Eat(string contents, ref int index)
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
			
			if (!dot)
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