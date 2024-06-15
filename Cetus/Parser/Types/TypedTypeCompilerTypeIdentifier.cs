using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerTypeIdentifier : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("Compiler list does not have an LLVM type");
	public string Name => "TypeIdentifier";
	public override string ToString() => Name;
}