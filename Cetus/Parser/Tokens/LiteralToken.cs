﻿namespace Cetus.Parser.Tokens;

public class LiteralToken(string token) : IToken
{
	public bool Eat(string contents, ref int index)
	{
		if (contents[index..].StartsWith(token))
		{
			index += token.Length;
			return true;
		}
		
		return false;
	}
	
	public override string ToString() => token;
}

public class Semicolon() : LiteralToken(";");