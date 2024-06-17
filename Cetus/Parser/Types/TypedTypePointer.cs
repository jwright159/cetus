using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypePointer : TypedTypeWithInnerType
{
	public TypedTypePointer(TypedType innerType)
	{
		InnerType = innerType;
	}
	
	public TypedTypePointer() { }
	
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(InnerType.LLVMType, 0);
	public string Name => "Pointer";
	public TypedType InnerType { get; set; }
	public override string ToString() => $"{Name}[{InnerType}]";
}