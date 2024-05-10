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
				throw new Exception($"Identifier '{valueName.TokenText}' not found");
			
			if (typeHint is not null and not TypedTypePointer && value.Type is TypedTypePointer resultTypePointer)
			{
				LLVMValueRef valueValue = builder.BuildLoad2(resultTypePointer.PointerType.LLVMType, value.Value, "loadtmp");
				value = new TypedValueValue(resultTypePointer.PointerType, valueValue);
			}
			
			if (typeHint is not null && !TypedTypeExtensions.TypesEqual(value.Type, typeHint))
				throw new Exception($"Type mismatch in value of '{valueName.TokenText}', expected {typeHint.LLVMType} but got {value.Type.LLVMType}");
			
			return new Result.Ok();
		}
		else
		{
			value = null;
			return new Result.TokenRuleFailed("Expected value identifier", lexer.Line, lexer.Column);
		}
	}
}