using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseTypeIdentifier(IHasIdentifiers context, out TypedType? type)
	{
		if (lexer.Eat(out Word? typeName))
		{
			if (typeName.TokenText == "Closure")
			{
				Result innerTypeResult = ParseInnerTypeIdentifier(context, out TypedType? innerType);
				TypedTypeFunction functionType = new("block", innerType ?? VoidType, [new TypedTypePointer(new TypedTypeChar())], null, null);
				TypedTypeStruct closureStructType = new(LLVMTypeRef.CreateStruct([LLVMTypeRef.CreatePointer(functionType.LLVMType, 0), LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0)], false));
				type = new TypedTypeClosurePointer(closureStructType, functionType);
				return Result.WrapPassable("Expected inner type identifier", innerTypeResult);
			}
			if (typeName.TokenText == "CompilerClosure")
			{
				Result innerTypeResult = ParseInnerTypeIdentifier(context, out TypedType? innerType);
				type = new TypedTypeCompilerClosure(innerType ?? VoidType);
				return Result.WrapPassable("Expected inner type identifier", innerTypeResult);
			}
			else
			{
				if (!context.Identifiers.TryGetValue(typeName.TokenText, out TypedValue? result))
				{
					type = null;
					return Result.ComplexTokenRuleFailed($"Type '{typeName.TokenText}' not found", lexer.Line, lexer.Column);
				}
				type = result.Type;
				while (lexer.Eat<Dereference>())
					type = new TypedTypePointer(type);
				return new Result.Ok();
			}
		}
		else
		{
			type = null;
			return new Result.TokenRuleFailed("Expected type identifier", lexer.Line, lexer.Column);
		}
	}
}