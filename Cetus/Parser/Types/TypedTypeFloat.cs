using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFloat : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Float;
	public string Name => "Float";
	public override string ToString() => Name;
}