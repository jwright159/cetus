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
			integer = new IntegerContext { Value = hexIntegerToken.Value };
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
			integer = new IntegerContext { Value = decimalIntegerToken.Value };
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