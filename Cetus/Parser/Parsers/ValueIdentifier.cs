using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseValueIdentifier(FunctionContext context, TypedType? typeHint, out TypedValue? value)
	{
		if (lexer.Eat(out Word? valueName))
		{
			if (!context.Identifiers.TryGetValue(valueName.TokenText, out value))
				return new Result.TokenRuleFailed($"Identifier '{valueName.TokenText}' not found", lexer.Line, lexer.Column);
			
			if (typeHint is not null and not TypedTypePointer && value.Type is TypedTypePointer resultTypePointer)
			{
				LLVMValueRef valueValue = builder.BuildLoad2(resultTypePointer.PointerType.LLVMType, value.Value, "loadtmp");
				value = new TypedValueValue(resultTypePointer.PointerType, valueValue);
			}
			
			if (typeHint is not null && !value.IsOfType(typeHint))
				return Result.ComplexTokenRuleFailed($"Type mismatch in value of '{valueName.TokenText}', expected {typeHint} but got {value.Type}", lexer.Line, lexer.Column);
			
			return new Result.Ok();
		}
		else
		{
			value = null;
			return new Result.TokenRuleFailed("Expected value identifier", lexer.Line, lexer.Column);
		}
	}
}