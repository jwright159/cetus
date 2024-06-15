using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerAnyValue : TypedType
{
	public LLVMTypeRef LLVMType => throw new Exception("Generic value wrapper does not have an LLVM type");
	public string Name => "AnyValue";
	public override string ToString() => Name;
}