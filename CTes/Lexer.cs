using System.Diagnostics.CodeAnalysis;
using CTes.Tokens;

namespace CTes;

public class Lexer(string contents)
{
	public int Index = 0;
	
	public int Line => contents[..Index].Count(c => c == '\n') + 1;
	public int Column => contents[..Index].LastIndexOf('\n') is var i ? Index - i : Index + 1;
	
	public bool IsAtEnd => Index >= contents.Length;
	
	public bool Eat<T>([NotNullWhen(true)] out T? token) where T : IToken, new()
	{
		return IToken.TryParse(contents, ref Index, out token);
	}
	
	public bool Eat<T>() where T : IToken, new()
	{
		return IToken.TryParse(contents, ref Index, out T? _);
	}
}