using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerValue : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("Compiler value does not have an LLVM type");
	public string Name => "CompilerValue";
	public override string ToString() => Name;
}