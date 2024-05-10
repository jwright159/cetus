using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseIncludeLibrary()
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Include>() &&
		    lexer.Eat(out Word? libraryName))
		{
			referencedLibs.Add(libraryName.TokenText);
			if (lexer.SkipTo<Semicolon>())
				return new Result.ComplexRuleFailed("Invalid include", new Result.TokenRuleFailed("Expected ';'", lexer.Line, lexer.Column));
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Invalid include", lexer.Line, lexer.Column);
		}
	}
}