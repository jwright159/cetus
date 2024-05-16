using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class NullContext : IValueContext;

public partial class Parser
{
	public Result ParseNull(out NullContext @null)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<Null>())
		{
			@null = new NullContext();
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

public partial class Visitor
{
	public TypedValue VisitNull(NullContext @null, TypedType? typeHint)
	{
		if (typeHint == null)
			throw new Exception("Cannot infer type of null");
		return new TypedValueValue(typeHint, LLVMValueRef.CreateConstNull(typeHint.LLVMType));
	}
}