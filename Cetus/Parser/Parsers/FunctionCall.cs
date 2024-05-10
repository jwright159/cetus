using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionCall(FunctionContext context, out TypedValue? value)
	{
		int startIndex = lexer.Index;
		if (ParseExpression(context, null, out TypedValue? function, 4) is Result.Passable functionResult)
		{
			int argumentStartIndex = lexer.Index;
			if (!lexer.Eat<LeftParenthesis>())
			{
				lexer.Index = startIndex;
				value = null;
				return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
			}
			lexer.Index = argumentStartIndex;
			
			List<TypedValue> arguments = [];
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
				functionType = (TypedTypeFunction)functionPointer.PointerType;
				function = new TypedValueValue(functionType, builder.BuildLoad2(functionType.LLVMType, function.Value, "functionValue"));
			}
			else
				functionType = (TypedTypeFunction)function.Type;
			
			if (ParseFunctionArguments(context, functionType.ParamTypes.ToArray(), functionType.VarArgType, out List<TypedValue>? restArguments) is not Result.Passable argumentsResult)
			{
				lexer.Index = startIndex;
				value = null;
				return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
			}
			arguments.AddRange(restArguments);
			
			if (functionType.IsVarArg ? arguments.Count < functionType.NumParams : arguments.Count != functionType.NumParams)
				throw new Exception($"Argument count mismatch in call to '{functionType.FunctionName}', expected {(functionType.IsVarArg ? "at least " : "")}{functionType.NumParams} but got {arguments.Count}");
			
			foreach ((TypedValue argument, TypedType type) in arguments.Zip(functionType.ParamTypes))
				if (!TypedTypeExtensions.TypesEqual(argument.Type, type))
					throw new Exception($"Argument type mismatch in call to '{functionType.FunctionName}', expected {type} but got {argument.Type.LLVMType}");
			
			value = functionType.Call(builder, function, context, arguments.ToArray());
			
			return Result.WrapPassable("Invalid function call", functionResult, argumentsResult);
		}
		else
		{
			lexer.Index = startIndex;
			value = null;
			return new Result.TokenRuleFailed("Expected function call", lexer.Line, lexer.Column);
		}
	}
}