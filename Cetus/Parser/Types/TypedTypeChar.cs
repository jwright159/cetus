using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeChar : TypedType
{
	public LLVMTypeRef LLVMType => LLVMTypeRef.Int8;
	public string Name => "Char";
	public override string ToString() => Name;
}