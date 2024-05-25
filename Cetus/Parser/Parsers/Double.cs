using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class DoubleContext : IValueContext
{
	public double Value;
}

public partial class Parser
{
	public Result ParseDouble(out DoubleContext @double)
	{
		if (lexer.Eat(out Tokens.Double? doubleToken))
		{
			@double = new DoubleContext { Value = doubleToken.Value };
			return new Result.Ok();
		}
		else
		{
			@double = null;
			return new Result.TokenRuleFailed("Expected double", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedValue VisitDouble(DoubleContext @double)
	{
		return new TypedValueValue(DoubleType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Double, @double.Value));
	}
}