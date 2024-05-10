using Cetus.Parser.Contexts;
using Cetus.Parser.Tokens;
using Cetus.Parser.Values;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseFunctionStatement(FunctionContext context)
	{
		int startIndex = lexer.Index;
		if (ParseFunctionCall(context, out TypedValue? _) is Result.Passable functionCallResult &&
		    lexer.Eat<Semicolon>())
		{
			return Result.WrapPassable("Invalid function statement", functionCallResult);
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected functionStatement", lexer.Line, lexer.Column);
		}
	}
}