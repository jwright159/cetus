using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseDouble(out TypedValue? @double)
	{
		if (lexer.Eat(out Tokens.Double? doubleToken))
		{
			double value = double.Parse(doubleToken.TokenText);
			@double = new TypedValueValue(DoubleType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, value));
			return new Result.Ok();
		}
		else
		{
			@double = null;
			return new Result.TokenRuleFailed("Expected double", lexer.Line, lexer.Column);
		}
	}
}