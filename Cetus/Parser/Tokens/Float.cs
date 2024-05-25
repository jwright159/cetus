namespace Cetus.Parser.Tokens;

public class Float : IToken
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
			
			if (contents[i] == 'f')
				i++;
			else
				return false;
			
			Value = float.Parse(contents[index..(i - 1)]);
			index = i;
			return true;
		}
		
		return false;
	}
		
	public float Value { get; private set; }
}