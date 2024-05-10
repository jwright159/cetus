using Cetus.Parser.Contexts;
using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public partial class Parser
{
	public Result? ParseValue(FunctionContext context, TypedType? typeHint, out TypedValue? value)
	{
		if (lexer.IsAtEnd)
		{
			value = null;
			return null;
		}
		Result result = ParseHexInteger(out value) as Result.Passable ??
		                ParseFloat(out value) as Result.Passable ??
		                ParseDouble(out value) as Result.Passable ??
		                ParseDecimalInteger(out value) as Result.Passable ??
		                ParseString(typeHint, out value) as Result.Passable ??
		                ParseClosure(context, typeHint, out value) as Result.Passable ??
		                ParseNull(typeHint, out value) as Result.Passable ??
		                ParseValueIdentifier(context, typeHint, out value) as Result.Passable ??
		                new Result.TokenRuleFailed("Expected value", lexer.Line, lexer.Column) as Result;
		return Result.WrapPassable("Invalid value", result);
	}
}