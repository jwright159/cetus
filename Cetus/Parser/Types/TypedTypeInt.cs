using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeInt : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int32;
	public string Name => "Int";
	public override string ToString() => Name;
}