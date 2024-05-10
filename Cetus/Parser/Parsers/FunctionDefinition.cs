using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionDefinition(ProgramContext context)
	{
		int startIndex = lexer.Index;
		if (
			ParseTypeIdentifier(context, out TypedType? returnType) is Result.Passable returnTypeResult &&
			lexer.Eat(out Word? functionName) &&
			ParseFunctionParameters(context, out FunctionParameters? parameters) is Result.Passable parametersResult)
		{
			string name = functionName.TokenText;
			TypedTypeFunction functionType = new(name, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg?.Type, null);
			TypedValue function = new TypedValueValue(functionType, module.AddFunction(name, functionType.LLVMType));
			LLVMValueRef functionValue = function.Value;
			context.Identifiers.Add(name, function);
			
			functionValue.Linkage = LLVMLinkage.LLVMExternalLinkage;

			FunctionContext functionContext = new(context);
			
			for (int i = 0; i < parameters.Parameters.Count; ++i)
			{
				string parameterName = parameters.Parameters[i].Name;
				TypedType parameterType = parameters.Parameters[i].Type;
				LLVMValueRef param = functionValue.GetParam((uint)i);
				param.Name = parameterName;
				functionContext.Identifiers.Add(parameterName, new TypedValueValue(parameterType, param));
			}
			
			builder.PositionAtEnd(functionValue.AppendBasicBlock("entry"));
			
			Result functionBlockResult = ParseFunctionBlock(functionContext);
			
			return Result.WrapPassable("Invalid function definition", returnTypeResult, parametersResult, functionBlockResult);
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected function definition", lexer.Line, lexer.Column);
		}
	}
}