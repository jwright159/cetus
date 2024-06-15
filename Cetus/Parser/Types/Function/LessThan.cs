using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class LessThan() : TypedTypeFunctionSimple("LessThan", Visitor.IntType, [(Visitor.IntType, "a"), (Visitor.IntType, "b")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args["a"].LLVMValue, args["b"].LLVMValue, "lttmp");
	}
}