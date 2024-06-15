using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Tokens;

public class ValueIdentifierContext : IToken, TypedValue
{
	public string Name { get; private set; }
	public TypedValue Value;
	
	public TypedType Type { get; }
	public LLVMValueRef LLVMValue { get; }
	
	public Result Eat(Lexer lexer)
	{
		Result result = lexer.Eat(out Word word);
		if (result is Result.Passable)
			Name = word.Value;
		return result;
	}
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		if (typeHint is TypedTypeCompilerString)
		{
			Value = new TypedValueCompiler<string>(typeHint, Name);
			return;
		}
		
		if (!context.Identifiers.TryGetValue(Name, out Value))
			throw new Exception($"Identifier '{Name}' not found");
		
		if (typeHint is not null)
			Value = Value.CoersePointer(typeHint, visitor, Name);
	}
}