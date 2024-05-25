using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class FunctionCall(string name, TypedType returnType, (TypedType Type, string Name)[] parameters, TypedType? varArgType) : TypedTypeFunction(name, returnType, parameters, varArgType)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		return new TypedValueValue(ReturnType, builder.BuildCall2(LLVMType, function.Value, args.Select(arg => arg.Value).ToArray(), ReturnType is TypedTypeVoid ? "" : Name + "Call"));
	}
}