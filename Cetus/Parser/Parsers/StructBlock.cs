using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public class StructFieldContext
{
	public TypeIdentifierContext TypeIdentifier;
	public TypedType Type;
	public string Name;
}

public partial class Parser
{
	public Result ParseStructBlock(out List<StructFieldContext> fields)
	{
		int startIndex = lexer.Index;
		if (lexer.Eat<LeftBrace>())
		{
			List<Result> results = [];
			fields = [];
			while (ParseTypeIdentifier(out TypeIdentifierContext type) is Result.Passable typeResult
			       && lexer.Eat(out Word? name))
			{
				StructFieldContext field = new();
				field.TypeIdentifier = type;
				field.Name = name.TokenText;
				fields.Add(field);
				if (typeResult is Result.Failure)
					results.Add(typeResult);
				
				lexer.Eat<Semicolon>();
			}
			
			if (!lexer.Eat<RightBrace>())
			{
				results.Add(new Result.TokenRuleFailed("Expected '}'", lexer.Line, lexer.Column));
			}
			
			return Result.WrapPassable("Invalid struct block", results.ToArray());
		}
		else
		{
			lexer.Index = startIndex;
			fields = null;
			return new Result.TokenRuleFailed("Expected struct block", lexer.Line, lexer.Column);
		}
	}
}