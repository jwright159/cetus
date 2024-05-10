using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseDelegateDeclaration(ProgramContext context)
	{
		int startIndex = lexer.Index;
		if (
			lexer.Eat<Tokens.Delegate>() &&
			ParseTypeIdentifier(context, out TypedType? returnType) is Result.Passable typeIdentifierResult &&
			lexer.Eat(out Word? functionName) &&
			ParseFunctionParameters(context, out FunctionParameters? parameters) is Result.Passable functionParametersResult &&
			lexer.Eat<Semicolon>())
		{
			TypedTypeFunction functionType = new(functionName.TokenText, returnType, parameters.ParamTypes.ToArray(), parameters.VarArg.Type, null);
			TypedValue function = new TypedValueType(functionType);
			context.Identifiers.Add(functionName.TokenText, function);
			
			return Result.WrapPassable("Invalid delegate declaration", typeIdentifierResult, functionParametersResult);
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected delegate declaration", lexer.Line, lexer.Column);
		}
	}
}