using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseIncludeLibrary(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Include>() &&
		    lexer.Eat(out Word? libraryName))
		{
			program.Libraries.Add(libraryName.Value);
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected include library", lexer.Line, lexer.Column);
		}
	}
}