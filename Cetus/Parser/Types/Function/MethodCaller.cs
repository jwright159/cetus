using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class MethodCaller(TypedType @struct, TypedTypeFunction calledFunction) : TypedTypeFunction($"{@struct.Name}.Call_{calledFunction.Name}", new Lambda(calledFunction), [(@struct, "value")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		Lambda lambda = new(calledFunction, args[0]);
		return new TypedValueType(lambda);
	}
	
	private class Lambda(TypedTypeFunction calledFunction, TypedValue arg) : TypedTypeFunctionSimple($"{calledFunction.Name}_Lambda", calledFunction.ReturnType, calledFunction.Parameters.Skip(1).ToArray(), null)
	{
		public override LLVMValueRef Visit(IHasIdentifiers context, LLVMBuilderRef builder, TypedType? typeHint, FunctionArgs args)
		{
			return calledFunction.Call(builder, function, context, args.Prepend(arg).ToArray());
		}
	}
}