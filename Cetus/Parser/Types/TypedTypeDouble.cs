using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeDouble : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Double;
	public string Name => "Double";
	public override string ToString() => Name;
}