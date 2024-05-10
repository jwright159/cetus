using Cetus.Parser.Contexts;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public partial class Parser
{
	public Result? ParseExpression(FunctionContext context, TypedType? typeHint, out TypedValue? value, int order = 0)
	{
		if (lexer.IsAtEnd)
		{
			value = null;
			return null;
		}
		
		if (order <= 3)
		{
			if (ParseFunctionCall(context, out value) is Result.Passable functionCallResult)
			{
				return Result.WrapPassable("Invalid expression", functionCallResult);
			}
		}
		
		if (order <= 4)
		{
			if (ParseValue(context, typeHint, out value) is Result.Passable valueResult)
			{
				return Result.WrapPassable("Invalid expression", valueResult);
			}
		}
		
		value = null;
		return new Result.TokenRuleFailed("Expected expression", lexer.Line, lexer.Column);
	}
}