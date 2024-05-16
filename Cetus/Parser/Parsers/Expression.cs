using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public interface IExpressionContext;

public partial class Parser
{
	public Result ParseExpression(IHasIdentifiers program, out IExpressionContext expression, int order = 0)
	{
		if (ParseFunctionCall(program, out FunctionCallContext functionCall, order) is Result.Passable functionCallResult)
		{
			expression = functionCall;
			return Result.WrapPassable("Invalid expression", functionCallResult);
		}
		
		if (ParseValue(program, out IValueContext value) is Result.Passable valueResult)
		{
			expression = value;
			return Result.WrapPassable("Invalid expression", valueResult);
		}
		
		expression = null;
		return new Result.TokenRuleFailed("Expected expression", lexer.Line, lexer.Column);
	}
}

public partial class Visitor
{
	public TypedValue VisitExpression(IHasIdentifiers program, IExpressionContext expression, TypedType? typeHint)
	{
		if (expression is FunctionCallContext functionCall)
			return VisitFunctionCall(program, functionCall);
		if (expression is IValueContext value)
			return VisitValue(program, value, typeHint);
		throw new Exception("Unknown expression type {expression}");
	}
}