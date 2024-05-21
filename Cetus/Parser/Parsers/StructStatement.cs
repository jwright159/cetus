using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public interface IStructStatementContext;

public partial class Parser
{
	public Result ParseStructStatement(StructDefinitionContext @struct, out IStructStatementContext statement)
	{
		int startIndex = lexer.Index;
		if (ParseStructField(@struct, out StructFieldContext field) is Result.Passable fieldResult)
		{
			lexer.Eat<Semicolon>();
			statement = field;
			return Result.WrapPassable("Invalid function statement", fieldResult);
		}
		lexer.Index = startIndex;
		statement = null;
		return new Result.TokenRuleFailed("Expected function statement", lexer.Line, lexer.Column);
	}
}

public partial class Visitor
{
	public void VisitStructStatement(IHasIdentifiers program, IStructStatementContext statement)
	{
		if (statement is StructFieldContext field)
			VisitStructField(program, field);
		else
			throw new Exception($"Unknown function statement type {statement}");
	}
}