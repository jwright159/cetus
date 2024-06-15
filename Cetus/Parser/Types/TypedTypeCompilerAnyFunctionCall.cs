using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerAnyFunctionCall : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("AnyFunctionCall does not have an LLVM type");
	public string Name => "AnyFunctionCall";
	public override string ToString() => Name;
}