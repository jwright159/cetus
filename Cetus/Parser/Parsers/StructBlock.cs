using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseStructBlock(StructDefinitionContext @struct, out List<IStructStatementContext> statements)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<LeftBrace>())
		{
			List<Result> results = [];
			statements = [];
			while (ParseStructStatement(@struct, out IStructStatementContext statement) is Result.Passable typeResult)
			{
				statements.Add(statement);
				if (typeResult is Result.Failure)
					results.Add(typeResult);
			}
			
			if (!lexer.Eat<RightBrace>())
			{
				results.Add(new Result.TokenRuleFailed("Expected '}'", lexer.Line, lexer.Column));
			}
			
			return Result.WrapPassable("Invalid struct block", results.ToArray());
		}
		else
		{
			lexer.Index = startIndex;
			statements = null;
			return new Result.TokenRuleFailed("Expected struct block", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitStructBlock(IHasIdentifiers program, List<IStructStatementContext> statements)
	{
		foreach (IStructStatementContext statement in statements)
			VisitStructStatement(program, statement);
	}
}