using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueValue(TypedType type, LLVMValueRef value) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef LLVMValue => value;
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		
	}
	
	public override string ToString() => value.ToString();
}