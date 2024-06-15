using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeCompilerString : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
	public string Name => "CompilerString";
	public override string ToString() => Name;
}