using Cetus.Parser.Tokens;
using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class ValueIdentifierContext : IValueContext
{
	public string Name;
}

public partial class Parser
{
	public Result ParseValueIdentifier(out ValueIdentifierContext value)
	{
		if (lexer.Eat(out Word? valueName))
		{
			value = new ValueIdentifierContext { Name = valueName.TokenText };
			return new Result.Ok();
		}
		else
		{
			value = null;
			return new Result.TokenRuleFailed("Expected value identifier", lexer.Line, lexer.Column);
		}
	}
}

public partial class Visitor
{
	public TypedValue VisitValueIdentifier(IHasIdentifiers program, ValueIdentifierContext valueIdentifier, TypedType? typeHint)
	{
		string name = valueIdentifier.Name;
		
		if (!program.Identifiers.TryGetValue(name, out TypedValue? value))
			throw new Exception($"Identifier '{name}' not found");
		
		if (typeHint is not null)
			value = value.CoersePointer(typeHint, builder, name);
		
		return value;
	}
}