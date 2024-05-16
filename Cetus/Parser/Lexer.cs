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
	
	private bool TryParse<T>(ref T token) where T : IToken
	{
		if (Index == 0)
			EatWhitespace();
		
		if (IsAtEnd)
			return false;
		
		if (token.Eat(contents, ref Index))
		{
			EatWhitespace();
			return true;
		}
		return false;
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
		token = new T();
		return TryParse(ref token);
	}
	
	public bool Eat<T>() where T : IToken, new()
	{
		T token = new();
		return TryParse(ref token);
	}
	
	public bool Eat<T>(T token) where T : IToken
	{
		return TryParse(ref token);
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
	
	public bool EatMatches<TLeft, TRight>()
		where TLeft : IToken, new()
		where TRight : IToken, new()
	{
		int startIndex = Index;
		if (!Eat<TLeft>())
		{
			Index = startIndex;
			return false;
		}
		
		while (true)
		{
			if (IsAtEnd)
				return false;
			
			if (Eat<TRight>())
				return true;
			
			if (Eat<Tokens.String>() ||
				EatMatches<LeftParenthesis, RightParenthesis>() ||
			    EatMatches<LeftBrace, RightBrace>())
				continue;
			
			Index++;
			EatWhitespace();
		}
	}
}