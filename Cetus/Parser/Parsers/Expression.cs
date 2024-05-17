using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

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
		if (typeHint is TypedTypeCompilerExpression compilerExpressionType)
		{
			LLVMBasicBlockRef originalBlock = builder.InsertBlock;
			LLVMBasicBlockRef block = originalBlock.Parent.AppendBasicBlock("closureBlock");
			TypedValueCompilerExpression compilerExpression = new(compilerExpressionType, block);
			builder.PositionAtEnd(block);
			
			compilerExpression.ReturnValue = VisitExpression(program, expression, compilerExpressionType.ReturnType);
			
			builder.PositionAtEnd(originalBlock);
			
			return compilerExpression;
		}
		
		if (expression is FunctionCallContext functionCall)
			return VisitFunctionCall(program, functionCall, typeHint);
		if (expression is IValueContext value)
			return VisitValue(program, value, typeHint);
		throw new Exception("Unknown expression type {expression}");
	}
}