using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerAnyFunction : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("Generic function wrapper does not have an LLVM type");
	public string Name => "AnyFunction";
	public override string ToString() => Name;
}