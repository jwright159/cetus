using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class LessThan() : TypedTypeFunctionSimple("LessThan", Visitor.IntType, [(Visitor.IntType, "a"), (Visitor.IntType, "b")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		return builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, args["a"].LLVMValue, args["b"].LLVMValue, "lttmp");
	}
}