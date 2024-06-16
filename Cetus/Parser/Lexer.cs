using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public class Lexer(string contents)
{
	public int Index = 0;
	
	private int lastIndexAtLastLineCalculated = 0;
	private int lastLineCalculated = 1;
	public int Line
	{
		get
		{
			RecalculateLine();
			return lastLineCalculated;
		}
	}
	private int lastColumnCalculated = 1;
	public int Column
	{
		get
		{
			RecalculateLine();
			return lastColumnCalculated;
		}
	}
	
	public bool IsAtEnd => Index >= contents.Length;
	
	public string Contents => contents;
	
	private Result TryParse<T>(ref T token) where T : IToken
	{
		if (Index == 0)
			EatWhitespace();
		
		Result result = token.Eat(this);
		
		if (result is Result.Passable)
			EatWhitespace();
		
		return result;
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
	
	public Result Eat<T>(out T token) where T : IToken, new()
	{
		token = new T();
		return TryParse(ref token);
	}
	
	public Result Eat<T>() where T : IToken, new()
	{
		T token = new();
		return TryParse(ref token);
	}
	
	public Result Eat<T>(T token) where T : IToken
	{
		return TryParse(ref token);
	}
	
	public string EatTo<T>() where T : IToken, new()
	{
		int start = Index;
		while (!IsAtEnd && Eat<T>() is not Result.Passable) Index++;
		return contents[start..Index];
	}
	
	public string EatToMatches<T>(T token) where T : IToken
	{
		int start = Index;
		while (!IsAtEnd && Eat(token) is not Result.Passable)
			if (!EatAnyMatches())
				Index++;
		return contents[start..Index];
	}
	
	public string EatToMatches<T>() where T : IToken, new() => EatToMatches(new T());
	
	public Result? SkipToMatches<T>(T token, bool failIfSkip = true) where T : IToken
	{
		int originalIndex = Index;
		int originalLine = Line;
		int originalColumn = Column;
		if (Eat(token) is Result.Passable eatResult)
		{
			return eatResult;
		}
		else
		{
			EatToMatches(token);
			return failIfSkip ? Result.ComplexTokenRuleFailed($"Skipped to {token} because {contents[originalIndex]} was found", originalLine, originalColumn) : null;
		}
	}
	
	public Result? SkipToMatches<T>(bool failIfSkip = true) where T : IToken, new() => SkipToMatches(new T(), failIfSkip);
	
	public Result EatMatches<TLeft, TRight>(TLeft left, TRight right)
		where TLeft : IToken
		where TRight : IToken
	{
		if (Eat(left) is var startResult and not Result.Passable)
			return startResult;
		
		while (true)
		{
			if (IsAtEnd)
				return new Result.TokenRuleFailed($"Expected {typeof(TRight).Name}, got EOF", this);
			
			if (Eat(right) is Result.Passable endResult)
				return endResult;
			
			if (EatAnyMatches())
				continue;
			
			Index++;
			EatWhitespace();
		}
	}
	
	public Result EatMatches<TLeft, TRight>()
		where TLeft : IToken, new()
		where TRight : IToken, new()
		=> EatMatches(new TLeft(), new TRight());
	
	public bool EatAnyMatches()
	{
		return Eat<Tokens.String>() is Result.Passable ||
		       EatMatches(new LiteralToken("{"), new LiteralToken("}")) is Result.Passable ||
		       EatMatches(new LiteralToken("("), new LiteralToken(")")) is Result.Passable;
	}
	
	private void RecalculateLine()
	{
		if (Index > lastIndexAtLastLineCalculated)
		{
			int linesPassed = contents[lastIndexAtLastLineCalculated..Index].Count(c => c == '\n');
			if (linesPassed > 0)
			{
				lastLineCalculated += linesPassed;
				lastColumnCalculated = contents[..Index].LastIndexOf('\n') is var i && i < 0 ? Index + 1 : Index - i;
			}
			else
			{
				lastColumnCalculated += Index - lastIndexAtLastLineCalculated;
			}
			lastIndexAtLastLineCalculated = Index;
		}
		else if (Index < lastIndexAtLastLineCalculated)
		{
			int linesPassed = contents[Index..lastIndexAtLastLineCalculated].Count(c => c == '\n');
			if (linesPassed > 0)
			{
				lastLineCalculated -= linesPassed;
				lastColumnCalculated = contents[..Index].LastIndexOf('\n') is var i && i < 0 ? Index + 1 : Index - i;
			}
			else
			{
				lastColumnCalculated -= lastIndexAtLastLineCalculated - Index;
			}
			lastIndexAtLastLineCalculated = Index;
		}
	}
	
	public char this[int index] => contents[index];
	public string this[Range range] => contents[range];
	public int Length => contents.Length;
	public char Current => contents[Index];
	public bool StartsWith(string str) => contents[Index..].StartsWith(str);
	
	public override string ToString() => IsAtEnd ? "Lexer at EOF" : $"Lexer at \"{contents[Index..Math.Min(Index + 20, Length)]}...\"";
}