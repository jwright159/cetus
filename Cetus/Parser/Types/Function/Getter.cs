using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class Getter(GetterContext getter) : TypedTypeFunction(getter.Name, getter.Field.Type.Pointer(), [(getter.Struct.Type.Pointer(), "value")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		LLVMValueRef valuePtr = builder.BuildStructGEP2(getter.Struct.Type.LLVMType, args[0].Value, (uint)getter.Field.Index, getter.Field.Name + "Ptr");
		return new TypedValueValue(getter.Field.Type.Pointer(), valuePtr);
	}
}