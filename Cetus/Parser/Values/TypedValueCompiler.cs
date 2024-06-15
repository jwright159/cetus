using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public class TypedValueCompiler<TValue>(TypedType type, TValue value) : TypedValue
{
	public TypedType Type => type;
	public LLVMValueRef LLVMValue => throw new Exception("Cannot get the llvm value of a compiler value");
	public TValue CompilerValue => value;
	
	public void Parse(IHasIdentifiers context)
	{
		
	}
	
	public void Transform(IHasIdentifiers context, TypedType? typeHint)
	{
		
	}
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, LLVMBuilderRef builder)
	{
		
	}
}