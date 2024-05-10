using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeChar : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int8;
	public override string ToString() => "Char";
}