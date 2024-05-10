using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueCompilerString(string value) : TypedValue
{
	public TypedType Type => new TypedTypeCompilerString();
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a compiler string");
	public string StringValue => value;
	public override string ToString() => value;
}