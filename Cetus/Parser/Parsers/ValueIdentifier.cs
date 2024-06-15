using Cetus.Parser.Types;
using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser;

public class ValueIdentifierContext(string name) : TypedValue
{
	public string Name => name;
	public TypedValue Value;
	
	public TypedType Type { get; }
	public LLVMValueRef LLVMValue { get; }
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		if (typeHint is TypedTypeCompilerString)
		{
			Value = new TypedValueCompiler<string>(typeHint, Name);
			return;
		}
		
		if (!context.Identifiers.TryGetValue(Name, out Value))
			throw new Exception($"Identifier '{Name}' not found");
		
		if (typeHint is not null)
			Value = Value.CoersePointer(typeHint, builder, Name);
	}
}