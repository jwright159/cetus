using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionBlock(IHasIdentifiers program, out List<IFunctionStatementContext> statements)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<LeftBrace>())
		{
			List<Result> results = [];
			statements = [];
			while (ParseFunctionStatement(program, out IFunctionStatementContext statement) is Result.Passable functionStatementResult)
			{
				statements.Add(statement);
				if (functionStatementResult is Result.Failure)
					results.Add(new Result.ComplexRuleFailed("Invalid function statement", functionStatementResult));
			}
			
			if (!lexer.Eat<RightBrace>())
			{
				results.Add(new Result.TokenRuleFailed("Expected '}'", lexer.Line, lexer.Column));
			}
			
			return Result.WrapPassable("Invalid function block", results.ToArray());
		}
		else
		{
			lexer.Index = startIndex;
			statements = null;
			return new Result.TokenRuleFailed("Expected function block", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitFunctionBlock(IHasIdentifiers program, List<IFunctionStatementContext> statements)
	{
		foreach (IFunctionStatementContext statement in statements)
			VisitFunctionStatement(program, statement);
	}
}