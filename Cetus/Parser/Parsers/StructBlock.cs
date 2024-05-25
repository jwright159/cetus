using Cetus.Parser.Tokens;

namespace Cetus.Parser;

public partial class Parser
{
	public Result ParseStructBlockFirstPass(StructDefinitionContext @struct)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<LeftBrace>())
		{
			List<Result> results = [];
			while (ParseStructStatementFirstPass(@struct) is Result.Passable typeResult)
			{
				if (typeResult is Result.Failure)
					results.Add(typeResult);
			}
			
			if (lexer.SkipToMatches<RightBrace>(out int line, out int column))
				results.Add(new Result.TokenRuleFailed("Expected '}'", line, column));
			
			return Result.WrapPassable("Invalid struct block", results.ToArray());
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected struct block", lexer.Line, lexer.Column);
		}
	}
}