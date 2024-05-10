using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypePointer(TypedType pointerType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(pointerType.LLVMType, 0);
	public TypedType PointerType => pointerType;
	public override string ToString() => pointerType + "*";
}