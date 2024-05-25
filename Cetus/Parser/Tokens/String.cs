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
				return false;
			
			Value = System.Text.RegularExpressions.Regex.Unescape(contents[(index + 1)..(i - 1)]);
			index = i;
			return true;
		}
		
		return false;
	}
	
	public string Value { get; private set; }
}