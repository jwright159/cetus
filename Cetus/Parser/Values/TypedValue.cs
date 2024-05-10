using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public interface TypedValue
{
	public TypedType Type { get; }
	public LLVMValueRef Value { get; }
}