using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionArguments(FunctionContext context, TypedType[] paramTypes, TypedType? varArgType, out List<TypedValue>? arguments)
	{
		if (lexer.Eat<LeftParenthesis>())
		{
			List<Result> results = [];
			arguments = [];
			int paramIndex = 0;
			while (ParseExpression(context, paramIndex < paramTypes.Length ? paramTypes[paramIndex++] : varArgType, out TypedValue? argument) is Result.Passable functionParameterResult)
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