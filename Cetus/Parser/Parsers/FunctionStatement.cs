using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public interface IFunctionStatementContext;

public partial class Parser
{
	public Result ParseFunctionStatement(IHasIdentifiers program, out IFunctionStatementContext statement)
	{
		int startIndex = lexer.Index;
		if (ParseFunctionCall(program, out FunctionCallContext functionCall) is Result.Passable functionCallResult)
		{
			statement = functionCall;
			
			Result? semicolonResult = null;
			if (lexer.SkipTo<Semicolon>(out int line, out int column))
				semicolonResult = new Result.TokenRuleFailed("Expected ';'", line, column);
			
			return Result.WrapPassable("Invalid function statement", functionCallResult, semicolonResult);
		}
		else
		{
			lexer.Index = startIndex;
			statement = null;
			return new Result.TokenRuleFailed("Expected function statement", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitFunctionStatement(IHasIdentifiers program, IFunctionStatementContext statement)
	{
		if (statement is FunctionCallContext functionCall)
			VisitFunctionCall(program, functionCall);
		else
			throw new Exception($"Unknown function statement type {statement}");
	}
}