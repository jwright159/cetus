using Cetus.Parser.Types;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseString(TypedType? typeHint, out TypedValue? @string)
	{
		if (lexer.Eat(out Tokens.String? stringToken))
		{
			string value = stringToken.TokenText[1..^1];
			value = System.Text.RegularExpressions.Regex.Unescape(value);
			@string = typeHint is TypedTypeCompilerString
				? new TypedValueCompilerString(value)
				: new TypedValueValue(StringType, builder.BuildGlobalStringPtr(value, value.Length == 0 ? "emptyString" : value));
			return new Result.Ok();
		}
		else
		{
			@string = null;
			return new Result.TokenRuleFailed("Expected string", lexer.Line, lexer.Column);
		}
	}
}