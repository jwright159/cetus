using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public bool ParseIncludeLibrary(ProgramContext program)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Include>() &&
		    lexer.Eat(out Word? libraryName) &&
		    lexer.Eat<Semicolon>())
		{
			program.Libraries.Add(libraryName.TokenText);
			return true;
		}
		else
		{
			lexer.Index = startIndex;
			return false;
		}
	}
}