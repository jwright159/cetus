using Cetus.Parser.Types;
using LLVMSharp.Interop;

namespace Cetus.Parser.Values;

public interface TypedValue
{
	public TypedType Type { get; }
	public LLVMValueRef LLVMValue { get; }
	public void Parse(IHasIdentifiers context);
	public void Transform(IHasIdentifiers context, TypedType? typeHint);
	public void Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor);
}