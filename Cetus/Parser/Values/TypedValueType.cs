using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueType(TypedType type) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef LLVMValue => throw new Exception("Cannot get the value of a type");
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		if (type is ITypedTypeRequiresVisit requiresVisit)
			requiresVisit.Visit(context, visitor);
	}
	
	public override string ToString() => type.ToString()!;
}