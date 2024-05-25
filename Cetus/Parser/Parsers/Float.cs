using Cetus.Parser.Tokens;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class FloatContext : IValueContext
{
	public float Value;
}

public partial class Parser
{
	public Result ParseFloat(out FloatContext @float)
	{
		if (lexer.Eat(out Float? floatToken))
		{
			@float = new FloatContext { Value = floatToken.Value };
			return new Result.Ok();
		}
		else
		{
			@float = null;
			return new Result.TokenRuleFailed("Expected float", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedValue VisitFloat(FloatContext @float)
	{
		return new TypedValueValue(FloatType, LLVMValueRef.CreateConstReal(LLVMTypeRef.Float, @float.Value));
	}
}