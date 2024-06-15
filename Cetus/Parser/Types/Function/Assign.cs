using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Assign() : TypedTypeFunctionSimple("Assign", Visitor.VoidType, [(Visitor.IntType.Pointer(), "target"), (Visitor.IntType, "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildStore(args["value"].LLVMValue, args["target"].LLVMValue);
	}
}