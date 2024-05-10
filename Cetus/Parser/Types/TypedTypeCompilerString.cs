using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerString : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
	public override string ToString() => "CompilerString";
}