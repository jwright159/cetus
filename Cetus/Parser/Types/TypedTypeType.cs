using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeType : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("Type type has no LLVM type");
	public string Name => "Type";
	public override string ToString() => Name;
}