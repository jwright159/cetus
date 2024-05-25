using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public interface IStructStatementContext;

public partial class Parser
{
	public Result ParseStructStatementFirstPass(StructDefinitionContext @struct)
	{
		int startIndex = lexer.Index;
		if (ParseFunctionDefinitionFirstPass(@struct) is Result.Passable functionResult)
		{
			lexer.Eat<Semicolon>();
			return functionResult;
		}
		if (ParseStructFieldFirstPass(@struct) is Result.Passable fieldResult)
		{
			lexer.Eat<Semicolon>();
			return fieldResult;
		}
		lexer.Index = startIndex;
		return new Result.TokenRuleFailed("Expected function statement", lexer.Line, lexer.Column);
	}
}