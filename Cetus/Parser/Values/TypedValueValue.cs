using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueValue(TypedType type, LLVMValueRef value) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => value;
	public override string ToString() => value.ToString();
}