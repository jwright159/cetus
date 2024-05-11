namespace Cetus.Parser.Tokens;

public class DecimalInteger : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (char.IsDigit(contents[index]))
		{
			int i = index;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
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

public class HexInteger : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (contents.Length > index + 2 && contents[index..(index+2)] == "0x")
		{
			int i = index + 2;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
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