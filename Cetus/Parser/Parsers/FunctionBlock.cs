using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionBlock(FunctionContext context)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<LeftBrace>())
		{
			List<Result> results = [];
			while (ParseFunctionStatement(context) is Result.Passable functionStatementResult)
			{
				if (functionStatementResult is Result.Failure)
					results.Add(new Result.ComplexRuleFailed("Invalid function statement", functionStatementResult));
			}
			
			if (!lexer.Eat<RightBrace>())
			{
				results.Add(new Result.ComplexRuleFailed("Expected '}'", new Result.TokenRuleFailed("Expected '}'", lexer.Line, lexer.Column)));
			}
			
			return Result.WrapPassable("Invalid function block", results.ToArray());
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected function block", lexer.Line, lexer.Column);
		}
	}
}