using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeVoid : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Void;
	public override string ToString() => "Void";
}