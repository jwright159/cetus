using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
	public int PointerDepth => 0;
	public TypedType BaseType => this;
}