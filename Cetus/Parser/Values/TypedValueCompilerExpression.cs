using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueCompilerExpression(TypedTypeCompilerExpression type, LLVMBasicBlockRef block) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef Value => throw new Exception("Cannot get the value of a compiler expression");
	public LLVMBasicBlockRef Block => block;
	public TypedValue ReturnValue;
}