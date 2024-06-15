using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Add() : TypedTypeFunctionSimple("Add", Visitor.IntType, [(Visitor.IntType, "a"), (Visitor.IntType, "b")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		return builder.BuildAdd(args["a"].LLVMValue, args["b"].LLVMValue, "addtmp");
	}
}