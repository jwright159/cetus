using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Getter(GetterContext getter) : TypedTypeFunctionSimple(getter.Name, getter.Field.Type.Pointer(), [(getter.Struct.Pointer(), "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		return builder.BuildStructGEP2(getter.Struct.LLVMType, args["value"].LLVMValue, (uint)getter.Field.Index, getter.Field.Name + "Ptr");
	}
}