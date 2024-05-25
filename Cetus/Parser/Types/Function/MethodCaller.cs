using Cetus.Parser.Values;
using LLVMSharp.Interop;

namespace Cetus.Parser.Types.Function;

public class MethodCaller(StructDefinitionContext @struct, TypedTypeFunction calledFunction) : TypedTypeFunction($"{@struct.Name}.Call_{calledFunction.Name}", new Lambda(calledFunction), [(@struct.Type, "value")], null)
{
	public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
	{
		Lambda lambda = new(calledFunction)
		{
			Arg = args[0]
		};
		return new TypedValueType(lambda);
	}
	
	private class Lambda(TypedTypeFunction calledFunction) : TypedTypeFunction($"{calledFunction.Name}_Lambda", calledFunction.ReturnType, calledFunction.Parameters.Skip(1).ToArray(), null)
	{
		public TypedValue Arg { get; init; }
		
		public override TypedValue Call(LLVMBuilderRef builder, TypedValue function, IHasIdentifiers context, params TypedValue[] args)
		{
			return calledFunction.Call(builder, function, context, args.Prepend(Arg).ToArray());
		}
	}
}