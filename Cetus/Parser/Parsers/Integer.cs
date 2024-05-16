using System.Globalization;
using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class IntegerContext : IValueContext
{
	public int Value;
}

public partial class Parser
{
	public Result ParseHexInteger(out IntegerContext integer)
	{
		if (lexer.Eat(out HexInteger? hexIntegerToken))
		{
			int value = int.Parse(hexIntegerToken.TokenText[2..], NumberStyles.HexNumber);
			integer = new IntegerContext { Value = value };
			return new Result.Ok();
		}
		else
		{
			integer = null;
			return new Result.TokenRuleFailed("Expected integer", lexer.Line, lexer.Column);
		}
	}
	
	public Result ParseDecimalInteger(out IntegerContext integer)
	{
		if (lexer.Eat(out DecimalInteger? decimalIntegerToken))
		{
			int value = int.Parse(decimalIntegerToken.TokenText);
			integer = new IntegerContext { Value = value };
			return new Result.Ok();
		}
		else
		{
			integer = null;
			return new Result.TokenRuleFailed("Expected integer", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedValue VisitInteger(IntegerContext integer)
	{
		return new TypedValueValue(IntType, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)integer.Value, true));
	}
}