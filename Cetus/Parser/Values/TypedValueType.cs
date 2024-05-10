using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueType(TypedType type) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a type");
	public override string ToString() => type.ToString()!;
}