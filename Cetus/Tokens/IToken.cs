using System.Diagnostics.CodeAnalysis;

namespace Cetus.Tokens;

public interface IToken
{
	public static abstract bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token);
	public string TokenText { get; init; }
		
	public static bool TryParse<T>(string contents, ref int index, [NotNullWhen(true)] out T? token) where T : IToken, new()
	{
		if (T.Split(contents, ref index, out string? tokenText))
		{
			token = new T { TokenText = tokenText };
			while (index < contents.Length && char.IsWhiteSpace(contents[index])) index++;
			return true;
		}
		else
		{
			token = default;
			return false;
		}
	}
}