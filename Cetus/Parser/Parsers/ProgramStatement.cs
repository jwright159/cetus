using Cetus.Parser.Contexts;

namespace Cetus.Parser;

public partial class Parser
{
	public Result? ParseProgramStatement(ProgramContext context)
	{
		if (lexer.IsAtEnd)
    		return null;
		Result result = ParseIncludeLibrary() as Result.Passable ??
		                ParseFunctionDefinition(context) as Result.Passable ??
		                ParseExternFunctionDeclaration(context) as Result.Passable ??
		                ParseExternStructDeclaration(context) as Result.Passable ??
		                ParseDelegateDeclaration(context) as Result.Passable ??
		                new Result.TokenRuleFailed("Expected program statement", lexer.Line, lexer.Column) as Result;
		return result is Result.Ok ? result : new Result.ComplexRuleFailed("Invalid program statement", result);
	}
}