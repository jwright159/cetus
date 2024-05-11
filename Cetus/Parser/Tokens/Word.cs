namespace Cetus.Parser.Tokens;

public class Word : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (char.IsLetter(contents[index]) || contents[index] == '_')
		{
			int i = index;
			while (i < contents.Length && (char.IsLetterOrDigit(contents[i]) || contents[i] == '_')) i++;
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