using System.Diagnostics.CodeAnalysis;
using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public class Lexer(string contents)
{
	public int Index = 0;
	
	public int Line => contents[..Index].Count(c => c == '\n') + 1;
	public int Column => contents[..Index].LastIndexOf('\n') is var i ? Index - i : Index + 1;
	
	public bool IsAtEnd => Index >= contents.Length;
	
	public string Contents => contents;
	
	public bool TryParse<T>([NotNullWhen(true)] out T? token) where T : IToken, new()
	{
		if (Index == 0)
			EatWhitespace();
		
		if (T.Split(contents, ref Index, out string? tokenText))
		{
			token = new T { TokenText = tokenText };
			EatWhitespace();
			return true;
		}
		else
		{
			token = default;
			return false;
		}
	}
	
	private void EatWhitespace()
	{
		while (true)
		{
			if (IsAtEnd)
				break;
			
			if (char.IsWhiteSpace(contents[Index]))
			{
				Index++;
				continue;
			}
			
			if (contents[Index] == '/' && contents[Index + 1] == '/')
			{
				while (!IsAtEnd && contents[Index] != '\n') Index++;
				continue;
			}
			
			break;
		}
	}
	
	public bool Eat<T>([NotNullWhen(true)] out T? token) where T : IToken, new()
	{
		return TryParse(out token);
	}
	
	public bool Eat<T>() where T : IToken, new()
	{
		return TryParse(out T? _);
	}
	
	public string EatTo<T>() where T : IToken, new()
	{
		int start = Index;
		while (!IsAtEnd && !Eat<T>()) Index++;
		return contents[start..Index];
	}
	
	/// <returns>True if any tokens were skipped</returns>
	public bool SkipTo<T>() where T : IToken, new()
	{
		if (Eat<T>())
			return false;
		
		EatTo<T>();
		return true;
	}
}