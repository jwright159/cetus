using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Return() : TypedTypeFunctionSimple("Return", Visitor.VoidType, [(Visitor.IntType, "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		return args["value"] is null ? builder.BuildRetVoid() : builder.BuildRet(args["value"].LLVMValue);
	}
}