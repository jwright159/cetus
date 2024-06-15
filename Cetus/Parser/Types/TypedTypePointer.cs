using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypePointer(TypedType baseType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(baseType.LLVMType, 0);
	public string Name => "Pointer";
	public TypedType InnerType => baseType;
	public override string ToString() => Name;
}