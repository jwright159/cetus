using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Return() : TypedTypeFunctionSimple("Return", Visitor.VoidType, [(Visitor.IntType, "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return args["value"] is null ? visitor.Builder.BuildRetVoid() : visitor.Builder.BuildRet(args["value"].LLVMValue);
	}
}