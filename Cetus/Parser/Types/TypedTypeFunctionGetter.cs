using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionGetter(GetterContext getter) : TypedTypeFunction($"{getter.Struct.Name}.Get_{getter.Field.Name}", getter.Field.Type.Pointer(), [getter.Struct.Type.Pointer()], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		LLVMValueRef valuePtr = builder.BuildStructGEP2(getter.Struct.Type.LLVMType, args[0].Value, (uint)getter.Field.Index, getter.Field.Name + "Ptr");
		return new TypedValueValue(getter.Field.Type.Pointer(), valuePtr);
	}
}