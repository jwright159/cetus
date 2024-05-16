using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypePointer(TypedType baseType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(baseType.LLVMType, 0);
	public TypedType BaseType => baseType;
	public int PointerDepth => baseType.PointerDepth + 1;
	public override string ToString() => baseType + "*";
}