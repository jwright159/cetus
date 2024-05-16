using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public class StringContext : IValueContext
{
	public string Value;
}

public partial class Parser
{
	public Result ParseString(out StringContext @string)
	{
		if (lexer.Eat(out Tokens.String? stringToken))
		{
			string value = stringToken.TokenText[1..^1];
			value = System.Text.RegularExpressions.Regex.Unescape(value);
			@string = new StringContext { Value = value };
			return new Result.Ok();
		}
		else
		{
			@string = null;
			return new Result.TokenRuleFailed("Expected string", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedValue VisitString(StringContext @string, TypedType? typeHint)
	{
		return typeHint is TypedTypeCompilerString
			? new TypedValueCompilerString(@string.Value)
			: new TypedValueValue(StringType, builder.BuildGlobalStringPtr(@string.Value, @string.Value.Length == 0 ? "emptyString" : @string.Value));
	}
}