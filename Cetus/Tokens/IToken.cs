using System.Diagnostics.CodeAnalysis;

namespace Cetus.Tokens;

public interface IToken
{
	public static abstract bool Split(string contents, ref int index, [NotNullWhen(true)] out string? token);
	public string TokenText { get; init; }
}