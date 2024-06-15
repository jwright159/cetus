using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeVoid : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Void;
	public string Name => "Void";
	public override string ToString() => Name;
}