using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Getter(GetterContext getter) : TypedTypeFunctionSimple(getter.Name, getter.Field.Type.Pointer(), [(getter.Struct.Pointer(), "value")], null)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildStructGEP2(getter.Struct.LLVMType, args["value"].LLVMValue, (uint)getter.Field.Index, getter.Field.Name + "Ptr");
	}
}