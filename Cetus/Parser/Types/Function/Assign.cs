using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Assign() : TypedTypeFunctionSimple("Assign", Visitor.VoidType, [(Visitor.IntType.Pointer(), "target"), (Visitor.IntType, "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		return builder.BuildStore(args["value"].LLVMValue, args["target"].LLVMValue);
	}
}