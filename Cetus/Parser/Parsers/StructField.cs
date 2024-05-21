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
}

public partial class Parser
{
	public Result ParseStructField(StructDefinitionContext @struct, out StructFieldContext field)
	{
		int startIndex = lexer.Index;
		if (ParseTypeIdentifier(out TypeIdentifierContext type) is Result.Passable typeResult
			       && lexer.Eat(out Word? name))
		{
			field = new StructFieldContext();
			field.TypeIdentifier = type;
			field.Name = name.TokenText;
			field.Index = @struct.Fields.Count;
			field.Getter = new GetterContext(@struct, field);
			@struct.Fields.Add(field);
			((NestedCollection<IFunctionContext>)@struct.Functions).SuperList.Add(field.Getter);
			return Result.WrapPassable("Invalid struct field", typeResult);
		}
		else
		{
			lexer.Index = startIndex;
			field = null;
			return new Result.TokenRuleFailed("Expected struct field", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public void VisitStructField(IHasIdentifiers program, StructFieldContext field)
	{
		field.Type = VisitTypeIdentifier(program, field.TypeIdentifier);
		VisitGetter(field.Getter);
	}
}