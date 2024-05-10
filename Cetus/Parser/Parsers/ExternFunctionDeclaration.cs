using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseExternFunctionDeclaration(ProgramContext context)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Extern>() &&
			ParseTypeIdentifier(context, out TypedType? returnType) is Result.Passable typeIdentifierResult &&
			lexer.Eat(out Word? functionName) &&
			ParseFunctionParameters(context, out FunctionParameters? parameters) is Result.Passable functionParametersResult &&
			lexer.Eat<Semicolon>())
		{
			TypedTypeFunction functionType = new(functionName.TokenText, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type, null);
			LLVMValueRef functionValue = module.AddFunction(functionName.TokenText, functionType.LLVMType);
			TypedValue function = new TypedValueValue(functionType, functionValue);
			context.Identifiers.Add(functionName.TokenText, function);
			
			return Result.WrapPassable("Invalid extern function declaration", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected extern function declaration", lexer.Line, lexer.Column);
		}
	}
}