using Cetus.Parser.Tokens;
using Cetus.Parser.Types;

namespace Cetus.Parser;

public class StructFieldContext : IStructStatementContext
{
	public TypeIdentifierContext TypeIdentifier;
	public TypedType Type;
	public string Name;
	public int Index;
	public GetterContext Getter;
	
	public override string ToString() => $"{TypeIdentifier} {Name}";
}

public partial class Parser
{
	public Result ParseStructFieldFirstPass(StructDefinitionContext @struct)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(out TypeIdentifierContext type) is Result.Passable typeResult
			       && lexer.Eat(out Word? name))
		{
			StructFieldContext field = new();
			field.TypeIdentifier = type;
			field.Name = name.Value;
			field.Index = @struct.Fields.Count;
			field.Getter = new GetterContext(@struct, field);
			@struct.Fields.Add(field);
			return Result.WrapPassable("Invalid struct field", typeResult);
		}
		else
		{
			lexer.Index = startIndex;
			return new Result.TokenRuleFailed("Expected struct field", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitStructField(IHasIdentifiers program, StructFieldContext field)
	{
		field.Type = VisitTypeIdentifier(program, field.TypeIdentifier);
	}
}