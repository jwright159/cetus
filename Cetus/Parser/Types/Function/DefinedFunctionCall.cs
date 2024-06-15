using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class DefinedFunctionCall(string name, TypedType returnType, (TypedType Type, string Name)[] parameters, TypedType? varArgType) : TypedTypeFunctionSimple(name, returnType, parameters, varArgType)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
	{
		return builder.BuildCall2(LLVMType, function.LLVMValue, parameters.Select(param => args[param.Name].LLVMValue).ToArray(), ReturnType is TypedTypeVoid ? "" : Name + "Call");
	}
}