using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class DefinedFunctionCall(string name, TypedValue function, TypedType returnType, (TypedType Type, string Name)[] parameters, (TypedType Type, string Name)? varArg) : TypedTypeFunctionSimple(name, returnType, parameters, varArg)
{
	public override LLVMValueRef Visit(IHasIdentifiers context, TypedType? typeHint, Visitor visitor, FunctionArgs args)
	{
		return visitor.Builder.BuildCall2(LLVMType, function.LLVMValue, parameters.Select(param => args[param.Name].LLVMValue).ToArray(), ReturnType is TypedTypeVoid ? "" : Name + "Call");
	}
}