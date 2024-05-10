using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeClosurePointer(TypedTypeStruct type, TypedTypeFunction blockType) : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(type.LLVMType, 0);
	public TypedTypeStruct Type => type;
	public TypedTypeFunction BlockType => blockType;
	public override string ToString() => LLVMType.ToString();
}