using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class MethodGetter(StructDefinitionContext @struct, TypedTypeFunction returnFunction) : TypedTypeFunction($"{@struct.Name}.Get_{returnFunction.Name}", returnFunction, [(Visitor.TypeType, "type")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		return new TypedValueType(returnFunction);
	}
}