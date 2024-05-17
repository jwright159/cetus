using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types;

public class TypedTypeFunctionCall(string name, TypedType returnType, TypedType[] paramTypes, TypedType? varArgType) : TypedTypeFunction(name, returnType, paramTypes, varArgType)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		return new TypedValueValue(ReturnType, builder.BuildCall2(LLVMType, function.Value, args.Select(arg => arg.Value).ToArray(), ReturnType is TypedTypeVoid ? "" : Name + "Call"));
	}
}