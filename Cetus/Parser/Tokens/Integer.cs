namespace Cetus.Parser.Tokens;

public class DecimalInteger : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (char.IsDigit(contents[index]))
		{
			int i = index;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
			Value = int.Parse(contents[index..i]);
			index = i;
			return true;
		}
		
		return false;
	}
	
	public int Value { get; private set; }
}

public class HexInteger : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (contents.Length > index + 2 && contents[index..(index+2)] == "0x")
		{
			int i = index + 2;
			while (i < contents.Length && char.IsDigit(contents[i])) i++;
			Value = int.Parse(contents[(index + 1)..i]);
			index = i;
			return true;
		}
		
		return false;
	}
	
	public int Value { get; private set; }
}