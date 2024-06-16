using System.Collections;
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
	
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor)
	{
		
	}
	
	public override string ToString() => CompilerValue is IEnumerable enumerable ? "[\n\t" + string.Join(",\n", StringsOf(enumerable)).Replace("\n", "\n\t") + "\n]" : CompilerValue.ToString();
	
	private static IEnumerable<string> StringsOf(IEnumerable enumerable) => from object? item in enumerable select item.ToString();
}