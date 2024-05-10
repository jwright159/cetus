using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFloat(out TypedValue? @float)
	{
		if (lexer.Eat(out Float? floatToken))
		{
			float value = float.Parse(floatToken.TokenText[..^1]);
			@float = new TypedValueValue(FloatType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, value));
			return new Result.Ok();
		}
		else
		{
			@float = null;
			return new Result.TokenRuleFailed("Expected float", lexer.Line, lexer.Column);
		}
	}
}