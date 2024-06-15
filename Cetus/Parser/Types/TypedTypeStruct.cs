using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeStruct(LLVMTypeRef type) : TypedType
{
	public LLVMTypeRef LLVMType => type;
	public string Name => LLVMType.ToString();
	public override string ToString() => Name;
}