using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeStruct(LLVMTypeRef type) : TypedType
{
	public LLVMTypeRef LLVMType => type;
	public override string ToString() => LLVMType.ToString();
}