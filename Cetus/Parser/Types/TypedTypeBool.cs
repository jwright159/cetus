using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeBool : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int1;
	public override string ToString() => "Bool";
}