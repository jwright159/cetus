using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionGetter(StructDefinitionContext structDefinition, StructFieldContext field, int index) : TypedTypeFunction($"{structDefinition.Name}.Get_{field.Name}", field.Type.Pointer(), [structDefinition.Type.Pointer()], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		LLVMValueRef valuePtr = builder.BuildStructGEP2(structDefinition.Type.LLVMType, args[0].Value, (uint)index, field.Name + "Ptr");
		return new TypedValueValue(field.Type.Pointer(), valuePtr);
	}
}