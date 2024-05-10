using System.Globalization;
using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseHexInteger(out TypedValue? integer)
	{
		if (lexer.Eat(out HexInteger? hexIntegerToken))
		{
			int value = int.Parse(hexIntegerToken.TokenText[2..], NumberStyles.HexNumber);
			integer = new TypedValueValue(IntType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)value, true));
			return new Result.Ok();
		}
		else
		{
			integer = null;
			return new Result.TokenRuleFailed("Expected integer", lexer.Line, lexer.Column);
		}
	}
	
	public Result ParseDecimalInteger(out TypedValue? integer)
	{
		if (lexer.Eat(out DecimalInteger? decimalIntegerToken))
		{
			int value = int.Parse(decimalIntegerToken.TokenText);
			integer = new TypedValueValue(IntType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)value, true));
			return new Result.Ok();
		}
		else
		{
			integer = null;
			return new Result.TokenRuleFailed("Expected integer", lexer.Line, lexer.Column);
		}
	}
}