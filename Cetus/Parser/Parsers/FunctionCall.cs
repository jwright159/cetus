using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class FunctionCallContext : IFunctionStatementContext, IExpressionContext
{
	public IFunctionContext? Context;
	public IValueContext? Value;
	public List<IExpressionContext> Arguments = [];
}

public partial class Parser
{
	public Result ParseFunctionCall(IHasIdentifiers program, out FunctionCallContext functionCall, int order = 0)
	{
		int startIndex = lexer.Index;
		
		foreach (IFunctionContext patternFunction in program.Functions
			         .Where(value => value is { Pattern.Length: > 0 })
			         .Skip(order))
		{
			order++;
			IToken[] pattern = patternFunction.Pattern;
			IExpressionContext[] arguments = new IExpressionContext[patternFunction.ParameterContexts.Parameters.Count];
			
			bool match = true;
			foreach (IToken token in pattern)
			{
				if (token is ParameterExpressionToken expressionIndex)
				{
					if (ParseExpression(program, out IExpressionContext expression, order) is Result.Failure)
					{
						match = false;
						break;
					}
					arguments[expressionIndex.Index] = expression;
				}
				else if (token is ParameterValueToken valueIndex)
				{
					if (ParseValueIdentifier(out ValueIdentifierContext value) is Result.Failure)
					{
						match = false;
						break;
					}
					arguments[valueIndex.Index] = value;
				}
				else
				{
					if (!lexer.Eat(token))
					{
						match = false;
						break;
					}
				}
			}
			if (!match)
			{
				lexer.Index = startIndex;
				continue;
			}
			
			functionCall = new FunctionCallContext();
			functionCall.Context = patternFunction;
			functionCall.Arguments = arguments.ToList();
			
			return new Result.Ok();
		}
		
		if (ParseValue(program, out IValueContext? function) is Result.Passable functionResult)
		{
			int argumentStartIndex = lexer.Index;
			if (!lexer.Eat<LeftParenthesis>())
			{
				lexer.Index = startIndex;
				functionCall = null;
				return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
			}
			lexer.Index = argumentStartIndex;
			
			if (ParseFunctionArguments(program, out List<IExpressionContext>? arguments) is not Result.Passable argumentsResult)
			{
				lexer.Index = startIndex;
				functionCall = null;
				return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
			}
			
			functionCall = new FunctionCallContext();
			functionCall.Value = function;
			functionCall.Arguments = arguments;
			
			return Result.WrapPassable("Invalid function call", functionResult, argumentsResult);
		}
		else
		{
			lexer.Index = startIndex;
			functionCall = null;
			return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedValue VisitFunctionCall(IHasIdentifiers program, FunctionCallContext functionCall, TypedType? typeHint)
	{
		List<TypedValue> arguments = [];
		TypedValue function = functionCall.Context is not null ? functionCall.Context.Value : VisitValue(program, functionCall.Value, null);
		TypedTypeFunction functionType;
		if (function.Type is TypedTypeClosurePointer closurePtr)
		{
			functionType = closurePtr.BlockType;
				
			LLVMValueRef functionPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.Value, 0, "functionPtrPtr");
			LLVMValueRef functionPtr = builder.BuildLoad2(functionType.Pointer().LLVMType, functionPtrPtr, "functionPtr");
			LLVMValueRef environmentPtrPtr = builder.BuildStructGEP2(closurePtr.Type.LLVMType, function.Value, 1, "environmentPtrPtr");
			LLVMValueRef environmentPtr = builder.BuildLoad2(CharType.Pointer().LLVMType, environmentPtrPtr, "environmentPtr");
				
			function = new TypedValueValue(functionType, functionPtr);
			arguments.Add(new TypedValueValue(CharType.Pointer(), environmentPtr));
		}
		else if (function.Type is TypedTypePointer functionPointer)
		{
			functionType = (TypedTypeFunction)functionPointer.BaseType;
			function = new TypedValueValue(functionType, builder.BuildLoad2(functionType.LLVMType, function.Value, "functionValue"));
		}
		else
			functionType = (TypedTypeFunction)function.Type;
		
		TypedType varArgType = functionType.VarArgType;
		arguments.AddRange(functionCall.Arguments
			.Enumerate((paramIndex, arg) => VisitExpression(program, arg, paramIndex < functionType.ParamTypes.Length ? functionType.ParamTypes[paramIndex] : varArgType)));
		
		if (functionType.IsVarArg ? arguments.Count < functionType.NumParams : arguments.Count != functionType.NumParams)
			throw new Exception($"Argument count mismatch in call to '{functionType.Name}', expected {(functionType.IsVarArg ? "at least " : "")}{functionType.NumParams} but got {arguments.Count}");
		
		foreach ((TypedValue argument, TypedType type) in arguments.Zip(functionType.ParamTypes))
			if (!argument.IsOfType(type))
				throw new Exception($"Argument type mismatch in call to '{functionType.Name}', expected {type} but got {argument.Type.LLVMType}");
		
		TypedValue result = functionType.Call(builder, function, program, arguments.ToArray());
		
		if (typeHint is not null)
			result = result.CoersePointer(typeHint, builder, functionType.Name);
		
		return result;
	}
}

internal static class EnumerableExtensions
{
	public static IEnumerable<TResult> Enumerate<TSource, TResult>(this IEnumerable<TSource> source, Func<int, TSource, TResult> selector, int index = 0)
	{
		foreach (TSource item in source)
			yield return selector(index++, item);
	}
	
	public static IEnumerable<(int, TSource)> Enumerate<TSource>(this IEnumerable<TSource> source, int index = 0)
	{
		foreach (TSource item in source)
			yield return (index++, item);
	}
}