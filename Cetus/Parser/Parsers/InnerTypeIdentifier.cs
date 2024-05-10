using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseInnerTypeIdentifier(IHasIdentifiers context, out TypedType? innerType)
	{
		if (!lexer.Eat<LeftTriangle>())
		{
			innerType = null;
			return new Result.TokenRuleFailed("Expected inner type identifier", lexer.Line, lexer.Column);
		}
		
		if (ParseTypeIdentifier(context, out innerType) is not Result.Passable)
			return new Result.ComplexRuleFailed("Expected inner type identifier", new Result.TokenRuleFailed("Expected inner type identifier", lexer.Line, lexer.Column));
		
		if (lexer.SkipTo<RightTriangle>())
			return new Result.ComplexRuleFailed("Expected '>'",  new Result.TokenRuleFailed("Expected '>'", lexer.Line, lexer.Column));
		
		return new Result.Ok();
	}
}