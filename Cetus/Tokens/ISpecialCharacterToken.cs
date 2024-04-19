using System.Diagnostics.CodeAnalysis;

namespace Cetus.Tokens;

public interface ISpecialCharacterToken<T> : IToken where T : ISpecialCharacterToken<T>
{
	public static abstract string SpecialToken { get; }
	
	static bool IToken.Split(string contents, ref int index, [NotNullWhen(true)] out string? token)
	{
		if (contents[index..].StartsWith(T.SpecialToken))
		{
			token = T.SpecialToken;
			index += T.SpecialToken.Length;
			return true;
		}
		else
		{
			token = null;
			return false;
		}
	}
}