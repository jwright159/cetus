using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseNull(TypedType? typeHint, out TypedValue? @null)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Null>())
		{
			if (typeHint == null)
				throw new Exception("Cannot infer type of null");
			@null = new TypedValueValue(typeHint, LLVMValueRef.CreateConstNull(typeHint.LLVMType));
			return new Result.Ok();
		}
		else
		{
			lexer.Index = startIndex;
			@null = null;
			return new Result.TokenRuleFailed("Expected null", lexer.Line, lexer.Column);
		}
	}
}