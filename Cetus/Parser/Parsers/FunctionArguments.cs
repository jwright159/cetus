using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionArguments(IHasIdentifiers program, out List<IExpressionContext> arguments)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			List<Result> results = [];
			arguments = [];
			while (ParseExpression(program, out IExpressionContext? argument) is Result.Passable functionParameterResult)
			{
				if (functionParameterResult is Result.Failure)
					results.Add(functionParameterResult);
				arguments.Add(argument);
				if (!lexer.Eat<Comma>())
					break;
			}
			if (lexer.SkipTo<RightParenthesis>())
				results.Add(Result.ComplexTokenRuleFailed("Expected ')'", lexer.Line, lexer.Column));
			return Result.WrapPassable("Invalid function arguments", results.ToArray());
		}
		else
		{
			arguments = null;
			return new Result.TokenRuleFailed("Expected '('", lexer.Line, lexer.Column);
		}
	}
}