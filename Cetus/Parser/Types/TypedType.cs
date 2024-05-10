using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public interface TypedType
{
	public LLVMTypeRef LLVMType { get; }
}